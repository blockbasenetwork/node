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

            _logger.LogInformation("Automatic Production running. Node will automatically send candidatures to sidechains that meet the required conditions");

            var networkInfo = await _mainchainService.GetInfo();
            _networkName = EosNetworkNames.GetNetworkName(networkInfo.chain_id);

            _logger.LogInformation($"Looking for sidechains in network: {_networkName}");

            while (true)
            {
                try
                {
                    if (_sidechainKeeper.GetSidechains().Count() >= _providerConfigurations.AutomaticProduction.MaxNumberOfSidechains)
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
                            var checkResult = await CheckIfSidechainFitsRules(chainInCandidature.Name);
                            if (checkResult.found && await DoesVersionCheckOut(chainInCandidature.Name) && !IsSidechainRunning(chainInCandidature.Name))
                            {
                                _logger.LogInformation($"Found sidechain {chainInCandidature.Name} eligible for automatic production");

                                await DeleteSidechainIfExistsInDb(chainInCandidature.Name);
                                await _mongoDbProducerService.AddProducingSidechainToDatabaseAsync(chainInCandidature.Name, checkResult.sidechainTimestamp, true);

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

                try
                {
                    var sidechainsInNode = _sidechainKeeper.GetSidechains();

                    foreach (var sidechainInNode in sidechainsInNode)
                    {
                        var sidechainInDb = await _mongoDbProducerService.GetProducingSidechainAsync(sidechainInNode.SidechainPool.ClientAccountName, sidechainInNode.SidechainPool.SidechainCreationTimestamp);
                        var pastSidechain = await _mongoDbProducerService.GetPastSidechainAsync(sidechainInNode.SidechainPool.ClientAccountName, sidechainInNode.SidechainPool.SidechainCreationTimestamp);
                        if (pastSidechain?.ReasonLeft == LeaveNetworkReasonsConstants.EXIT_REQUEST || (sidechainInDb != null && !sidechainInDb.IsAutomatic)) continue;

                        var sidechainStillFitsRules = await CheckIfSidechainFitsRules(sidechainInNode.SidechainPool.ClientAccountName);
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
                decimal.TryParse(bbtBalanceString, out bbtBalance);
            }

            if (accountStake?.Stake >= stake) return true;
            if (bbtBalance >= stake)
            {
                await _mainchainService.AddStake(sidechain, _nodeConfigurations.AccountName, stake.ToString("F4") + " BBT");
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

        private async Task<(bool found, int producerType, decimal stakeToPut, ulong sidechainTimestamp)> CheckIfSidechainFitsRules(string sidechain)
        {
            (bool found, int producerType, decimal stakeToPut, ulong sidechainTimestamp) defaultReturnValue = (false, 0, 0, 0);

            //gets the candidates first and only then the producers
            var candidates = await _mainchainService.RetrieveCandidates(sidechain);
            var producers = await _mainchainService.RetrieveProducersFromTable(sidechain);

            var contractInfo = await _mainchainService.RetrieveContractInformation(sidechain);
            var contractState = await _mainchainService.RetrieveContractState(sidechain);
            var clientInfo = await _mainchainService.RetrieveClientTable(sidechain);

            //verify if had access to chain information
            if (contractInfo == null || producers == null || candidates == null || contractState == null || clientInfo == null) return defaultReturnValue;

            if (contractInfo.BlockTimeDuration < 60 && _networkName == EosNetworkNames.MAINNET) return defaultReturnValue;

            //verify if node isn't in the candidates list nor the producers list
            if (candidates.Any(c => c.Key == _nodeConfigurations.AccountName) || producers.Any(p => p.Key == _nodeConfigurations.AccountName) || !contractState.CandidatureTime) return defaultReturnValue;

            if (!CheckIfSidechainFitsInMaxNumberOfSidechainsToProduce(_providerConfigurations.AutomaticProduction.MaxNumberOfSidechains)
                ||
                !CheckIfSidechainGrowthFitsInConfiguredMaximumGrowth(contractInfo, _providerConfigurations.AutomaticProduction.MaxGrowthPerMonthInMB)) return defaultReturnValue;

            decimal requestedStake = ConvertBBTValueToDecimalPoint(contractInfo.Stake);
            decimal maxStakeToPut = (decimal)_providerConfigurations.AutomaticProduction.MaxRatioToStake * requestedStake;
            decimal stakeToPut = 0;

            var maxSidechainGrowthPerMonthInMB = GetMaximumMonthlyGrowth(contractInfo.SizeOfBlockInBytes, (int)contractInfo.BlockTimeDuration);

            var producerTypeToCandidate = 0;
            decimal averagePaymentPerBlock = 0;

            if (
                _providerConfigurations.AutomaticProduction.FullNode.IsActive
                && _providerConfigurations.AutomaticProduction.FullNode.MaxSidechainGrowthPerMonthInMB >= maxSidechainGrowthPerMonthInMB
                && contractInfo.NumberOfFullProducersRequired > 0)
            {
                decimal maxStakeToMonthlyIncomeRatio = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.FullNode.MaxStakeToMonthlyIncomeRatio);
                decimal minPaymentExpectedPerBlock = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.FullNode.MinBBTPerEmptyBlock);
                decimal minBBTExpectedPerMB = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.FullNode.MinBBTPerMBRatio);
                decimal minPaymentPerBlock = Math.Round((decimal)contractInfo.MinPaymentPerBlockFullProducers / 10000, 4);
                decimal maxPaymentPerBlock = Math.Round((decimal)contractInfo.MaxPaymentPerBlockFullProducers / 10000, 4);

                var fitsAndPayments = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, requestedStake, minPaymentExpectedPerBlock, minBBTExpectedPerMB, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPayments.fits)
                {
                    producerTypeToCandidate = (int)ProducerTypeEnum.Full;
                    averagePaymentPerBlock = fitsAndPayments.averagePaymentPerBlock;
                    stakeToPut = requestedStake;
                }

                var fitsAndPaymentsForMaxStake = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, maxStakeToPut, minPaymentExpectedPerBlock, minBBTExpectedPerMB, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPaymentsForMaxStake.fits && maxStakeToPut > requestedStake)
                {
                    stakeToPut = maxStakeToPut;
                }
            }

            if (
                _providerConfigurations.AutomaticProduction.HistoryNode.IsActive
                && _providerConfigurations.AutomaticProduction.HistoryNode.MaxSidechainGrowthPerMonthInMB >= maxSidechainGrowthPerMonthInMB
                && contractInfo.NumberOfHistoryProducersRequired > 0)
            {
                decimal maxStakeToMonthlyIncomeRatio = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.HistoryNode.MaxStakeToMonthlyIncomeRatio);
                decimal minPaymentExpectedPerBlock = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.HistoryNode.MinBBTPerEmptyBlock);
                decimal minBBTExpectedPerMB = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.HistoryNode.MinBBTPerMBRatio);
                decimal minPaymentPerBlock = Math.Round((decimal)contractInfo.MinPaymentPerBlockHistoryProducers / 10000, 4);
                decimal maxPaymentPerBlock = Math.Round((decimal)contractInfo.MaxPaymentPerBlockHistoryProducers / 10000, 4);

                var fitsAndPayments = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, requestedStake, minPaymentExpectedPerBlock, minBBTExpectedPerMB, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPayments.fits && fitsAndPayments.averagePaymentPerBlock >= averagePaymentPerBlock)
                {
                    producerTypeToCandidate = (int)ProducerTypeEnum.History;
                    averagePaymentPerBlock = fitsAndPayments.averagePaymentPerBlock;
                    stakeToPut = requestedStake;
                }

                var fitsAndPaymentsForMaxStake = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, maxStakeToPut, minPaymentExpectedPerBlock, minBBTExpectedPerMB, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPaymentsForMaxStake.fits && maxStakeToPut > requestedStake)
                {
                    stakeToPut = maxStakeToPut;
                }
            }

            if (_providerConfigurations.AutomaticProduction.ValidatorNode.IsActive && contractInfo.NumberOfValidatorProducersRequired > 0)
            {
                decimal maxStakeToMonthlyIncomeRatio = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.ValidatorNode.MaxStakeToMonthlyIncomeRatio);
                decimal minPaymentExpectedPerBlock = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.ValidatorNode.MinBBTPerEmptyBlock);
                decimal minBBTExpectedPerMB = Convert.ToDecimal(_providerConfigurations.AutomaticProduction.ValidatorNode.MinBBTPerMBRatio);
                decimal minPaymentPerBlock = Math.Round((decimal)contractInfo.MinPaymentPerBlockValidatorProducers / 10000, 4);
                decimal maxPaymentPerBlock = Math.Round((decimal)contractInfo.MaxPaymentPerBlockValidatorProducers / 10000, 4);

                var fitsAndPayments = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, requestedStake, minPaymentExpectedPerBlock, minBBTExpectedPerMB, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
                if (fitsAndPayments.fits && fitsAndPayments.averagePaymentPerBlock >= averagePaymentPerBlock)
                {
                    producerTypeToCandidate = (int)ProducerTypeEnum.Validator;
                    stakeToPut = requestedStake;
                }

                var fitsAndPaymentsForMaxStake = CheckIfRequestedProducerTypeFitsMinimumRequirements(maxStakeToMonthlyIncomeRatio, maxStakeToPut, minPaymentExpectedPerBlock, minBBTExpectedPerMB, minPaymentPerBlock, maxPaymentPerBlock, contractInfo.BlockTimeDuration, contractInfo.SizeOfBlockInBytes);
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

            return totalMaximumMonthlyGrowth <= maxTotalGrowthPerMonthInMB;
        }

        private bool CheckIfSidechainFitsInMaxNumberOfSidechainsToProduce(int maxNumberOfSidechainsToProduce)
        {
            var currentProducingSidechains = _sidechainKeeper.GetSidechains().Count();

            return currentProducingSidechains < maxNumberOfSidechainsToProduce;
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
    }
}