using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Endpoints;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Eos;
using BlockBase.Domain.Results;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Domain.Blockchain;
using BlockBase.Utils;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using System.Globalization;

namespace BlockBase.Runtime.Provider.AutomaticProduction
{
    public class AutomaticProductionManager : IAutomaticProductionManager
    {
        public TaskContainer AutomaticProductionTaskContainer { get; set; }
        private readonly SidechainKeeper _sidechainKeeper;
        private ISidechainProducerService _sidechainProducerService;
        private IMongoDbProducerService _mongoDbProducerService;
        private IMainchainService _mainchainService;
        private string _networkName;

        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;
        private ProviderConfigurations _providerConfigurations;

        private decimal _fullNodeMinBBTPerEmptyBlock;
        private decimal _fullNodeMinBBTPerMBRatio;
        private decimal _historyNodeMinBBTPerEmptyBlock;
        private decimal _historyNodeMinBBTPerMBRatio;
        private decimal _validatorNodeMinBBTPerEmptyBlock;
        private decimal _validatorNodeMinBBTPerMBRatio;

        private ILogger _logger;

        public AutomaticProductionManager(ILogger<IAutomaticProductionManager> logger, IMainchainService mainchainService, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, IOptions<ProviderConfigurations> providerConfigurations, IMongoDbProducerService mongoDbProducerService, ISidechainProducerService sidechainProducerService, SidechainKeeper sidechainKeeper)
        {
            _mongoDbProducerService = mongoDbProducerService;
            _mainchainService = mainchainService;
            _sidechainProducerService = sidechainProducerService;
            _sidechainKeeper = sidechainKeeper;

            _nodeConfigurations = nodeConfigurations.Value;
            _providerConfigurations = providerConfigurations.Value;
            _networkConfigurations = networkConfigurations.Value;

            _fullNodeMinBBTPerEmptyBlock = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.FullNode.MinBBTPerEmptyBlock);
            _fullNodeMinBBTPerMBRatio = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.FullNode.MinBBTPerMBRatio);
            _historyNodeMinBBTPerEmptyBlock = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.HistoryNode.MinBBTPerEmptyBlock);
            _historyNodeMinBBTPerMBRatio = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.HistoryNode.MinBBTPerMBRatio);
            _validatorNodeMinBBTPerEmptyBlock = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.ValidatorNode.MinBBTPerEmptyBlock);
            _validatorNodeMinBBTPerMBRatio = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.ValidatorNode.MinBBTPerMBRatio);

            _logger = logger;
        }

        public virtual TaskContainer Start()
        {
            if (AutomaticProductionTaskContainer != null) AutomaticProductionTaskContainer.Stop();

            AutomaticProductionTaskContainer = TaskContainer.Create(Run);
            AutomaticProductionTaskContainer.Start();
            return AutomaticProductionTaskContainer;
        }

        private async Task Run()
        {
            if (!_providerConfigurations.AutomaticProduction.ValidatorNode.IsActive &&
                !_providerConfigurations.AutomaticProduction.HistoryNode.IsActive &&
                !_providerConfigurations.AutomaticProduction.FullNode.IsActive)
                return;

            if (_providerConfigurations.AutomaticProduction.BBTValueAutoConfig)
            {
                await InitializeProviderMinValues();
            }

            _logger.LogInformation("Automatic Production running. Node will automatically send candidatures to sidechains that meet the required conditions");

            var networkInfo = await _mainchainService.GetInfo();
            _networkName = EosNetworkNames.GetNetworkName(networkInfo.chain_id);

            _logger.LogInformation($"Looking for sidechains in network: {_networkName}");

            while (true)
            {
                var sidechainsInNode = _sidechainKeeper.GetSidechains();

                try
                {
                    if (_providerConfigurations.AutomaticProduction.MaxNumberOfSidechains != 0 && _sidechainKeeper.GetSidechains().Count() >= _providerConfigurations.AutomaticProduction.MaxNumberOfSidechains)
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                    var activeSidechains = await GetSidechains(_networkName);
                    var chainsInCandidature = activeSidechains.Where(s => s.State == "Candidature").ToList();

                    if (chainsInCandidature.Any())
                    {
                        _logger.LogDebug("Found chains in candidature");
                        foreach (var chainInCandidature in chainsInCandidature)
                        {
                            if (sidechainsInNode.Any(s => s.SidechainPool.ClientAccountName == chainInCandidature.Name)) continue;
                            var checkResult = await CheckIfSidechainFitsRules(chainInCandidature.Name, true);
                            if (checkResult.found && await DoesVersionCheckOut(chainInCandidature.Name) && !IsSidechainRunning(chainInCandidature.Name))
                            {
                                _logger.LogInformation($"Found sidechain {chainInCandidature.Name} eligible for automatic production");

                                await DeleteSidechainIfExistsInDb(chainInCandidature.Name);
                                await _mongoDbProducerService.AddProducingSidechainToDatabaseAsync(chainInCandidature.Name, checkResult.sidechainTimestamp, true, checkResult.producerType);

                                if (await TryAddStakeIfNecessary(chainInCandidature.Name, checkResult.stakeToPut))
                                {
                                    await _sidechainProducerService.AddSidechainToProducerAndStartIt(chainInCandidature.Name, checkResult.sidechainTimestamp, checkResult.producerType, true);
                                }
                                else
                                {
                                    _logger.LogError($"Not enough BBT to stake for sidechain {chainInCandidature.Name}");
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Failed to automatically start producing chain with error: {e}");
                }

                if (_providerConfigurations.AutomaticProduction.AutomaticExitRequest)
                {
                    try
                    {
                        foreach (var sidechainInNode in sidechainsInNode)
                        {
                            var sidechainInDb = await _mongoDbProducerService.GetProducingSidechainAsync(sidechainInNode.SidechainPool.ClientAccountName, sidechainInNode.SidechainPool.SidechainCreationTimestamp);
                            if (sidechainInDb == null) continue;
                            if (!sidechainInDb.IsAutomatic) continue;

                            var producerTables = await _mainchainService.RetrieveProducersFromTable(sidechainInNode.SidechainPool.ClientAccountName);
                            var producerInTable = producerTables.Where(p => p.Key == _nodeConfigurations.AccountName).FirstOrDefault();
                            if (producerTables == null || producerInTable == null || (long)producerInTable.WorkTimeInSeconds <= ((DateTimeOffset)DateTime.UtcNow.AddDays(1)).ToUnixTimeSeconds()) continue;

                            var sidechainStillFitsRules = await CheckIfSidechainFitsRules(sidechainInNode.SidechainPool.ClientAccountName, false);
                            if (!sidechainStillFitsRules.found)
                            {
                                var trx = await _mainchainService.SidechainExitRequest(sidechainInNode.SidechainPool.ClientAccountName);
                                await _mongoDbProducerService.AddPastSidechainToDatabaseAsync(sidechainInNode.SidechainPool.ClientAccountName, sidechainInNode.SidechainPool.SidechainCreationTimestamp, false, LeaveNetworkReasonsConstants.EXIT_REQUEST);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Failed to check if sidechain still fits rules for production with error: {e}");
                    }
                }

                if (_providerConfigurations.AutomaticProduction.BBTValueAutoConfig) await UpdateMinValuesBasedOnCurrentValue();
                await Task.Delay(120000);
            }
        }

        private async Task<bool> TryAddStakeIfNecessary(string sidechain, decimal stake)
        {
            var accountStake = await _mainchainService.GetAccountStake(sidechain, _nodeConfigurations.AccountName);
            var bbtBalanceTable = await _mainchainService.GetCurrencyBalance(_networkConfigurations.BlockBaseTokenContract, _nodeConfigurations.AccountName, "BBT");
            decimal bbtBalance = 0;
            if (bbtBalanceTable != null)
            {
                var bbtBalanceString = bbtBalanceTable.FirstOrDefault()?.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                decimal.TryParse(bbtBalanceString, NumberStyles.Any, CultureInfo.InvariantCulture, out bbtBalance);
            }

            if (accountStake?.Stake >= stake) return true;
            if (bbtBalance >= stake)
            {
                await _mainchainService.AddStake(sidechain, _nodeConfigurations.AccountName, stake.ToString("F4", CultureInfo.InvariantCulture) + " BBT");
                return true;
            }

            return false;
        }

        private async Task<List<TrackerSidechain>> GetSidechains(string network)
        {
            var request = HttpHelper.ComposeWebRequestGet(BlockBaseNetworkEndpoints.GET_ALL_TRACKER_SIDECHAINS + $"?network={network}");
            var json = await HttpHelper.CallWebRequest(request);
            var trackerSidechains = JsonConvert.DeserializeObject<List<TrackerSidechain>>(json);

            return trackerSidechains;
        }

        private async Task<double> GetCurrentBBTValue()
        {
            var request = HttpHelper.ComposeWebRequestGet(BlockBaseNetworkEndpoints.GET_CURRENT_BBT_VALUE);
            var response = await HttpHelper.CallWebRequest(request);

            return JsonConvert.DeserializeObject<double>(response);
        }

        private async Task<(bool found, int producerType, decimal stakeToPut, ulong sidechainTimestamp)> CheckIfSidechainFitsRules(string sidechain, bool checkingToJoin)
        {
            (bool found, int producerType, decimal stakeToPut, ulong sidechainTimestamp) defaultReturnValue = (false, 0, 0, 0);

            //gets the candidates first and only then the producers
            var candidates = await _mainchainService.RetrieveCandidates(sidechain);
            var producers = await _mainchainService.RetrieveProducersFromTable(sidechain);

            var contractInfo = await _mainchainService.RetrieveContractInformation(sidechain);
            var contractState = await _mainchainService.RetrieveContractState(sidechain);
            var clientInfo = await _mainchainService.RetrieveClientTable(sidechain);

            //verify if had access to chain information
            if (contractInfo == null || producers == null || candidates == null || contractState == null || clientInfo == null)
                return checkingToJoin ? defaultReturnValue : (true, 0, 0, 0);

            //check if it's sidechain that provider had requested exit
            if (checkingToJoin && await CheckIfIsPastSidechainWithExitRequest(sidechain, clientInfo.SidechainCreationTimestamp)) return defaultReturnValue;

            if (contractInfo.BlockTimeDuration < 60 && _networkName == EosNetworkNames.MAINNET) return defaultReturnValue;

            //verify if chain is in candidature time when trying to join
            if (checkingToJoin && !contractState.CandidatureTime) return defaultReturnValue;

            //verify if node isn't in the candidates list nor the producers list
            if (checkingToJoin && (candidates.Any(c => c.Key == _nodeConfigurations.AccountName) || producers.Any(p => p.Key == _nodeConfigurations.AccountName))) return defaultReturnValue;

            if (!CheckIfSidechainGrowthFitsInConfiguredMaximumGrowth(contractInfo, _providerConfigurations.AutomaticProduction.MaxGrowthPerMonthInMB)) return defaultReturnValue;

            decimal requestedStake = ConvertBBTValueToDecimalPoint(contractInfo.Stake);
            decimal maxStakeToPut = (decimal)_providerConfigurations.AutomaticProduction.MaxRatioToStake * requestedStake;
            decimal stakeToPut = 0;

            var maxSidechainGrowthPerMonthInMB = GetMaximumMonthlyGrowth(contractInfo.SizeOfBlockInBytes, (int)contractInfo.BlockTimeDuration);

            var producerTypeToCandidate = 0;
            decimal averagePaymentPerBlock = 0;

            if (
                _providerConfigurations.AutomaticProduction.FullNode.IsActive
                && (_providerConfigurations.AutomaticProduction.FullNode.MaxSidechainGrowthPerMonthInMB == 0 || _providerConfigurations.AutomaticProduction.FullNode.MaxSidechainGrowthPerMonthInMB >= maxSidechainGrowthPerMonthInMB)
                && contractInfo.NumberOfFullProducersRequired > 0)
            {
                decimal maxStakeToMonthlyIncomeRatio = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.FullNode.MaxStakeToMonthlyIncomeRatio);
                decimal minPaymentPerBlock = Math.Round((decimal)contractInfo.MinPaymentPerBlockFullProducers / 10000, 4);
                decimal maxPaymentPerBlock = Math.Round((decimal)contractInfo.MaxPaymentPerBlockFullProducers / 10000, 4);

                var fitsAndPayments = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, requestedStake, _fullNodeMinBBTPerEmptyBlock, _fullNodeMinBBTPerMBRatio, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPayments.fits)
                {
                    producerTypeToCandidate = (int)ProducerTypeEnum.Full;
                    averagePaymentPerBlock = fitsAndPayments.averagePaymentPerBlock;
                    stakeToPut = requestedStake;
                }

                var fitsAndPaymentsForMaxStake = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, maxStakeToPut, _fullNodeMinBBTPerEmptyBlock, _fullNodeMinBBTPerMBRatio, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPaymentsForMaxStake.fits && maxStakeToPut > requestedStake)
                {
                    stakeToPut = maxStakeToPut;
                }
            }

            if (
                _providerConfigurations.AutomaticProduction.HistoryNode.IsActive
                && (_providerConfigurations.AutomaticProduction.HistoryNode.MaxSidechainGrowthPerMonthInMB == 0 || _providerConfigurations.AutomaticProduction.HistoryNode.MaxSidechainGrowthPerMonthInMB >= maxSidechainGrowthPerMonthInMB)
                && contractInfo.NumberOfHistoryProducersRequired > 0)
            {
                decimal maxStakeToMonthlyIncomeRatio = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.HistoryNode.MaxStakeToMonthlyIncomeRatio);
                decimal minPaymentPerBlock = Math.Round((decimal)contractInfo.MinPaymentPerBlockHistoryProducers / 10000, 4);
                decimal maxPaymentPerBlock = Math.Round((decimal)contractInfo.MaxPaymentPerBlockHistoryProducers / 10000, 4);

                var fitsAndPayments = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, requestedStake, _historyNodeMinBBTPerEmptyBlock, _historyNodeMinBBTPerMBRatio, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPayments.fits && fitsAndPayments.averagePaymentPerBlock >= averagePaymentPerBlock)
                {
                    producerTypeToCandidate = (int)ProducerTypeEnum.History;
                    averagePaymentPerBlock = fitsAndPayments.averagePaymentPerBlock;
                    stakeToPut = requestedStake;
                }

                var fitsAndPaymentsForMaxStake = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, maxStakeToPut, _historyNodeMinBBTPerEmptyBlock, _historyNodeMinBBTPerMBRatio, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPaymentsForMaxStake.fits && maxStakeToPut > requestedStake)
                {
                    stakeToPut = maxStakeToPut;
                }
            }

            if (_providerConfigurations.AutomaticProduction.ValidatorNode.IsActive && contractInfo.NumberOfValidatorProducersRequired > 0)
            {
                decimal maxStakeToMonthlyIncomeRatio = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.ValidatorNode.MaxStakeToMonthlyIncomeRatio);
                decimal minPaymentPerBlock = Math.Round((decimal)contractInfo.MinPaymentPerBlockValidatorProducers / 10000, 4);
                decimal maxPaymentPerBlock = Math.Round((decimal)contractInfo.MaxPaymentPerBlockValidatorProducers / 10000, 4);

                var fitsAndPayments = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, requestedStake, _validatorNodeMinBBTPerEmptyBlock, _validatorNodeMinBBTPerMBRatio, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPayments.fits && fitsAndPayments.averagePaymentPerBlock >= averagePaymentPerBlock)
                {
                    producerTypeToCandidate = (int)ProducerTypeEnum.Validator;
                    stakeToPut = requestedStake;
                }

                var fitsAndPaymentsForMaxStake = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, maxStakeToPut, _validatorNodeMinBBTPerEmptyBlock, _validatorNodeMinBBTPerMBRatio, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPaymentsForMaxStake.fits && maxStakeToPut > requestedStake)
                {
                    stakeToPut = maxStakeToPut;
                }
            }

            return (producerTypeToCandidate != 0, producerTypeToCandidate, stakeToPut, clientInfo.SidechainCreationTimestamp);
        }

        private (bool fits, decimal averagePaymentPerBlock) CheckIfRequestedProducerTypeFitsMinimumRequirements(decimal maxStakeToMonthlyIncomeRatio, decimal requestedStake, decimal minPaymentExpectedPerBlock, decimal minBBTExpectedPerMB, decimal minPaymentPerBlock, decimal maxPaymentPerBlock, uint blockTimeDurationInSeconds, uint blockSizeInBytes)
        {


            var fitAndRatio = CheckIfRequestedStakeFits(maxStakeToMonthlyIncomeRatio, requestedStake, minPaymentPerBlock, maxPaymentPerBlock, blockTimeDurationInSeconds);

            if (fitAndRatio.fits)
            {
                decimal minBlockSizeInMB = BlockHeaderSizeConstants.BLOCKHEADER_MAX_SIZE / 1000000;
                decimal maxBlockSizeInMB = Convert.ToDecimal(blockSizeInBytes / 1000000);

                var fits = CheckIfProvidedPaymentsFits(minPaymentPerBlock, maxPaymentPerBlock, minBlockSizeInMB, maxBlockSizeInMB, minPaymentExpectedPerBlock, minBBTExpectedPerMB);
                return (fits, (minPaymentPerBlock + maxPaymentPerBlock) / 2);
            }

            return (false, 0);
        }

        private decimal ConvertBBTValueToDecimalPoint(ulong value)
        {
            return Math.Round((decimal)value / 10000, 4);
        }

        private (bool fits, decimal stakeToIncomeRatio) CheckIfRequestedStakeFits(decimal maxStakeToMontlyIncomeRatio, decimal requestedStake, decimal minPaymentPerBlock, decimal maxPaymentPerBlock, uint blockTimeDurationInSeconds)
        {
            var stakeToIncomeRatio = GetStakeToMonthlyIncomeRatio(requestedStake, minPaymentPerBlock, maxPaymentPerBlock, blockTimeDurationInSeconds);
            return (maxStakeToMontlyIncomeRatio >= stakeToIncomeRatio, stakeToIncomeRatio);
        }

        private bool CheckIfProvidedPaymentsFits(decimal minimumPaymentOfferedPerBlock, decimal maximumPaymentOfferedPerBlock, decimal minBlockSizeInMB, decimal maxBlockSizeInMB, decimal minPaymentExpectedPerBlock, decimal minBBTExpectedPerMB)
        {
            var minPaymentCalculatedPerBlock = LinearFunc(minBBTExpectedPerMB, minPaymentExpectedPerBlock, minBlockSizeInMB, 0);
            var maxPaymentCalculatedPerBlock = LinearFunc(minBBTExpectedPerMB, minPaymentExpectedPerBlock, minBlockSizeInMB, maxBlockSizeInMB);

            return minPaymentCalculatedPerBlock <= minimumPaymentOfferedPerBlock && maxPaymentCalculatedPerBlock <= maximumPaymentOfferedPerBlock;
        }

        private decimal LinearFunc(decimal paymentRatio, decimal minPayment, decimal minBlockSize, decimal x)
        {
            return paymentRatio * (x + minBlockSize) + minPayment;
        }


        private bool CheckIfSidechainGrowthFitsInConfiguredMaximumGrowth(ContractInformationTable contractInfo, double maxTotalGrowthPerMonthInMB)
        {
            var maximumMonthlyGrowth = GetMaximumMonthlyGrowth(contractInfo.SizeOfBlockInBytes, (int)contractInfo.BlockTimeDuration);
            var totalMaximumMonthlyGrowth = maximumMonthlyGrowth;

            foreach (var runningSidechain in _sidechainKeeper.GetSidechains().ToList())
            {
                if (runningSidechain.SidechainStateManager.TaskContainer.IsRunning())
                {
                    totalMaximumMonthlyGrowth += GetMaximumMonthlyGrowth(runningSidechain.SidechainPool.BlockSizeInBytes, (int)runningSidechain.SidechainPool.BlockTimeDuration);
                }
            }

            return maxTotalGrowthPerMonthInMB == 0 ? true : totalMaximumMonthlyGrowth <= maxTotalGrowthPerMonthInMB;
        }

        private bool CheckIfSidechainFitsInMaxNumberOfSidechainsToProduce(int maxNumberOfSidechainsToProduce)
        {
            var currentProducingSidechains = _sidechainKeeper.GetSidechains().Count();

            return maxNumberOfSidechainsToProduce == 0 ? true : currentProducingSidechains < maxNumberOfSidechainsToProduce;
        }

        private decimal GetStakeToMonthlyIncomeRatio(decimal stake, decimal minPaymentPerBlock, decimal maxPaymentPerBlock, uint blockTimeDurationInSeconds)
        {
            var averagePaymentPerBlock = (minPaymentPerBlock + maxPaymentPerBlock) / 2;
            var blocksPerMonth = (decimal)2592000 / blockTimeDurationInSeconds;
            return (stake / (averagePaymentPerBlock * blocksPerMonth));
        }

        private double GetMaximumMonthlyGrowth(uint blockSizeInBytes, int blockTimeDuration)
        {
            var blockSizeInMbytes = (double)(blockSizeInBytes / 1000000);
            var blocksPerMonth = (double)2592000 / blockTimeDuration;
            return blockSizeInMbytes * blocksPerMonth;
        }

        private async Task<bool> DoesVersionCheckOut(string sidechain)
        {
            var softwareVersionString = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
            var softwareVersion = VersionHelper.ConvertFromVersionString(softwareVersionString);
            var versionInContract = await _mainchainService.RetrieveSidechainNodeVersion(sidechain);
            return softwareVersion >= versionInContract.SoftwareVersion;
        }

        private bool IsSidechainRunning(string sidechain)
        {
            var chainExistsInPool = _sidechainProducerService.DoesChainExist(sidechain);
            if (chainExistsInPool)
            {
                var sidechainContext = _sidechainProducerService.GetSidechainContext(sidechain);
                return sidechainContext.SidechainStateManager.TaskContainer.IsRunning();
            }
            return false;

        }

        private async Task DeleteSidechainIfExistsInDb(string sidechain)
        {
            var chainExistsInDb = await _mongoDbProducerService.CheckIfProducingSidechainAlreadyExists(sidechain);
            if (chainExistsInDb)
            {
                await _mongoDbProducerService.RemoveProducingSidechainFromDatabaseAsync(sidechain);
            }
        }

        private async Task<bool> CheckIfIsPastSidechainWithExitRequest(string sidechain, ulong sidechainCreationTime)
        {
            var pastSidechainInDb = await _mongoDbProducerService.GetPastSidechainAsync(sidechain, sidechainCreationTime);
            return (pastSidechainInDb != null && pastSidechainInDb.ReasonLeft == LeaveNetworkReasonsConstants.EXIT_REQUEST);
        }

        private async Task InitializeProviderMinValues()
        {
            var firstProviderMinValues = await _mongoDbProducerService.GetFirstProviderMinValues();

            if (firstProviderMinValues == null ||
                _fullNodeMinBBTPerEmptyBlock != firstProviderMinValues.FullNodeMinBBTPerEmptyBlock ||
                _fullNodeMinBBTPerMBRatio != firstProviderMinValues.FullNodeMinBBTPerMBRatio ||
                _historyNodeMinBBTPerEmptyBlock != firstProviderMinValues.HistoryNodeMinBBTPerEmptyBlock ||
                _historyNodeMinBBTPerMBRatio != firstProviderMinValues.HistoryNodeMinBBTPerMBRatio ||
                _validatorNodeMinBBTPerEmptyBlock != firstProviderMinValues.ValidatorNodeMinBBTPerEmptyBlock ||
                _validatorNodeMinBBTPerMBRatio != firstProviderMinValues.ValidatorNodeMinBBTPerMBRatio)
            {
                await _mongoDbProducerService.DropProviderMinValues();

                var providerMinValuesDB = new ProviderMinValuesDB()
                {
                    Timestamp = Convert.ToUInt64(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    FullNodeMinBBTPerEmptyBlock = _fullNodeMinBBTPerEmptyBlock,
                    FullNodeMinBBTPerMBRatio = _fullNodeMinBBTPerMBRatio,
                    HistoryNodeMinBBTPerEmptyBlock = _historyNodeMinBBTPerEmptyBlock,
                    HistoryNodeMinBBTPerMBRatio = _historyNodeMinBBTPerMBRatio,
                    ValidatorNodeMinBBTPerEmptyBlock = _validatorNodeMinBBTPerEmptyBlock,
                    ValidatorNodeMinBBTPerMBRatio = _validatorNodeMinBBTPerMBRatio
                };

                await _mongoDbProducerService.AddProviderMinValuesToDatabaseAsync(providerMinValuesDB);
            }
            else
            {
                var latestProviderMinValues = await _mongoDbProducerService.GetLatestProviderMinValues();

                _fullNodeMinBBTPerEmptyBlock = latestProviderMinValues.FullNodeMinBBTPerEmptyBlock;
                _fullNodeMinBBTPerMBRatio = latestProviderMinValues.FullNodeMinBBTPerMBRatio;
                _historyNodeMinBBTPerEmptyBlock = latestProviderMinValues.HistoryNodeMinBBTPerEmptyBlock;
                _historyNodeMinBBTPerMBRatio = latestProviderMinValues.HistoryNodeMinBBTPerMBRatio;
                _validatorNodeMinBBTPerEmptyBlock = latestProviderMinValues.ValidatorNodeMinBBTPerEmptyBlock;
                _validatorNodeMinBBTPerMBRatio = latestProviderMinValues.ValidatorNodeMinBBTPerMBRatio;
            }

            var latestStoredValue = await _mongoDbProducerService.GetLatestBBTValue();

            if (latestStoredValue == null)
            {
                var tokenCurrentValue = await GetCurrentBBTValue();
                await _mongoDbProducerService.AddBBTValueToDatabaseAsync(tokenCurrentValue);
            }
        }

        private async Task UpdateMinValuesBasedOnCurrentValue()
        {
            try
            {
                var latestStoredValue = await _mongoDbProducerService.GetLatestBBTValue();

                if (Convert.ToInt64(latestStoredValue.Timestamp) > DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds()) return;

                var tokenCurrentValue = await GetCurrentBBTValue();
                var convertedPreviousValue = Convert.ToDecimal(latestStoredValue.ValueInUSD);
                var convertedLatestValue = Convert.ToDecimal(tokenCurrentValue);

                if (_fullNodeMinBBTPerEmptyBlock != 0) _fullNodeMinBBTPerEmptyBlock = (_fullNodeMinBBTPerEmptyBlock * convertedPreviousValue) / convertedLatestValue;
                if (_fullNodeMinBBTPerMBRatio != 0) _fullNodeMinBBTPerMBRatio = (_fullNodeMinBBTPerMBRatio * convertedPreviousValue) / convertedLatestValue;
                if (_historyNodeMinBBTPerEmptyBlock != 0) _historyNodeMinBBTPerEmptyBlock = (_historyNodeMinBBTPerEmptyBlock * convertedPreviousValue) / convertedLatestValue;
                if (_historyNodeMinBBTPerMBRatio != 0) _historyNodeMinBBTPerMBRatio = (_historyNodeMinBBTPerMBRatio * convertedPreviousValue) / convertedLatestValue;
                if (_validatorNodeMinBBTPerEmptyBlock != 0) _validatorNodeMinBBTPerEmptyBlock = (_validatorNodeMinBBTPerEmptyBlock * convertedPreviousValue) / convertedLatestValue;
                if (_validatorNodeMinBBTPerMBRatio != 0) _validatorNodeMinBBTPerMBRatio = (_validatorNodeMinBBTPerMBRatio * convertedPreviousValue) / convertedLatestValue;

                var providerMinValuesDB = new ProviderMinValuesDB()
                {
                    Timestamp = Convert.ToUInt64(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    FullNodeMinBBTPerEmptyBlock = _fullNodeMinBBTPerEmptyBlock,
                    FullNodeMinBBTPerMBRatio = _fullNodeMinBBTPerMBRatio,
                    HistoryNodeMinBBTPerEmptyBlock = _historyNodeMinBBTPerEmptyBlock,
                    HistoryNodeMinBBTPerMBRatio = _historyNodeMinBBTPerMBRatio,
                    ValidatorNodeMinBBTPerEmptyBlock = _validatorNodeMinBBTPerEmptyBlock,
                    ValidatorNodeMinBBTPerMBRatio = _validatorNodeMinBBTPerMBRatio
                };

                await _mongoDbProducerService.AddProviderMinValuesToDatabaseAsync(providerMinValuesDB);
                await _mongoDbProducerService.AddBBTValueToDatabaseAsync(tokenCurrentValue);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to update BBT Value");
                _logger.LogDebug($"Exception: {e}");
            }
        }
    }
}