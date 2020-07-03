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
using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Network;
using BlockBase.Utils;
using BlockBase.Utils.Threading;
using EosSharp.Core.Exceptions;
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
            var networkName = EosNetworkNames.GetNetworkName(networkInfo.chain_id);

            _logger.LogInformation($"Looking for sidechains in network: {networkName}");

            while (true)
            {
                try
                {
                    if (_sidechainKeeper.GetSidechains().Count() >= _providerConfigurations.AutomaticProduction.MaxNumberOfSidechains)
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                    var activeSidechains = await GetSidechains(networkName);
                    var chainsInCandidature = activeSidechains.Where(s => s.State == "Candidature").ToList();

                    if (chainsInCandidature.Any())
                    {
                        _logger.LogDebug("Found chains in candidature");
                        foreach (var chainInCandidature in chainsInCandidature)
                        {
                            var checkResult = await CheckIfSidechainFitsRules(chainInCandidature);
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

                await Task.Delay(120000);
            }
        }

        private async Task<bool> TryAddStakeIfNecessary(string sidechain, decimal stake)
        {
            var accountStake = await _mainchainService.GetAccountStake(sidechain, _nodeConfigurations.AccountName);
            var bbtBalanceTable = await _mainchainService.GetCurrencyBalance(_networkConfigurations.BlockBaseTokenContract, _nodeConfigurations.AccountName, "BBT");
            decimal providerStake = 0;
            decimal bbtBalance = 0;
            if (accountStake != null)
            {
                var stakeString = accountStake.Stake?.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                decimal.TryParse(stakeString, out providerStake);
            }
            if (bbtBalanceTable != null)
            {
                var bbtBalanceString = bbtBalanceTable.FirstOrDefault()?.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                decimal.TryParse(bbtBalanceString, out bbtBalance);
            }
            
            if (providerStake >= stake) return true;
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

        private async Task<(bool found, int producerType, decimal stakeToPut, ulong sidechainTimestamp)> CheckIfSidechainFitsRules(TrackerSidechain sidechain)
        {
            var candidates = await _mainchainService.RetrieveCandidates(sidechain.Name);
            var producers = await _mainchainService.RetrieveProducersFromTable(sidechain.Name);
            var contractInfo = await _mainchainService.RetrieveContractInformation(sidechain.Name);
            var contractState = await _mainchainService.RetrieveContractState(sidechain.Name);
            var clientInfo = await _mainchainService.RetrieveClientTable(sidechain.Name);

            if (contractInfo == null || producers == null || candidates == null || contractState == null || clientInfo == null) return (false, 0, 0, 0);
            if (candidates.Any(c => c.Key == _nodeConfigurations.AccountName) || producers.Any(p => p.Key == _nodeConfigurations.AccountName) || !contractState.CandidatureTime) return (false, 0, 0, 0);

            var maximumMonthlyGrowth = GetMaximumMonthlyGrowth(contractInfo.SizeOfBlockInBytes, (int)contractInfo.BlockTimeDuration);
            var totalMaximumMonthlyGrowth = maximumMonthlyGrowth;

            foreach (var runningSidechain in _sidechainKeeper.GetSidechains().ToList())
            {
                if (runningSidechain.SidechainStateManager.TaskContainer.Task.Status == TaskStatus.Running)
                {
                    totalMaximumMonthlyGrowth += GetMaximumMonthlyGrowth(runningSidechain.SidechainPool.BlockSizeInBytes, (int)runningSidechain.SidechainPool.BlockTimeDuration);
                }
            }

            if (totalMaximumMonthlyGrowth > _providerConfigurations.AutomaticProduction.MaxGrowthPerMonthInMB) return (false, 0, 0, 0);

            int producerTypeToCandidate = 0;
            decimal lowestStakeToMonthlyIncomeRatio = decimal.MaxValue;
            decimal stakeToPut = Math.Round((decimal)contractInfo.Stake / 10000, 4);

            if (_providerConfigurations.AutomaticProduction.FullNode.IsActive && sidechain.FullProducers.RequiredNumberOfProducers > 0)
            {
                var stakeToMonthlyIncomeRatio = GetStakeToMonthlyIncomeRatio(stakeToPut, contractInfo.MinPaymentPerBlockFullProducers, contractInfo.MaxPaymentPerBlockFullProducers, (int)contractInfo.BlockTimeDuration);
                if (lowestStakeToMonthlyIncomeRatio >= stakeToMonthlyIncomeRatio &&
                    Convert.ToDecimal(_providerConfigurations.AutomaticProduction.FullNode.MaxStakeToMonthlyIncomeRatio) > stakeToMonthlyIncomeRatio &&
                    Math.Round((decimal)contractInfo.MinPaymentPerBlockFullProducers / 10000, 4) >= (decimal)_providerConfigurations.AutomaticProduction.FullNode.MinBBTPerBlock &&
                    _providerConfigurations.AutomaticProduction.FullNode.MaxSidechainGrowthPerMonthInMB > maximumMonthlyGrowth)
                {
                    lowestStakeToMonthlyIncomeRatio = stakeToMonthlyIncomeRatio;
                    producerTypeToCandidate = (int)ProducerTypeEnum.Full;
                }
            }

            if (_providerConfigurations.AutomaticProduction.HistoryNode.IsActive && sidechain.HistoryProducers.RequiredNumberOfProducers > 0)
            {
                var stakeToMonthlyIncomeRatio = GetStakeToMonthlyIncomeRatio(stakeToPut, contractInfo.MinPaymentPerBlockHistoryProducers, contractInfo.MaxPaymentPerBlockHistoryProducers, (int)contractInfo.BlockTimeDuration);
                if (lowestStakeToMonthlyIncomeRatio >= stakeToMonthlyIncomeRatio &&
                    Convert.ToDecimal(_providerConfigurations.AutomaticProduction.HistoryNode.MaxStakeToMonthlyIncomeRatio) > stakeToMonthlyIncomeRatio &&
                    Math.Round((decimal)contractInfo.MinPaymentPerBlockHistoryProducers / 10000, 4) >= (decimal)_providerConfigurations.AutomaticProduction.HistoryNode.MinBBTPerBlock &&
                    _providerConfigurations.AutomaticProduction.HistoryNode.MaxSidechainGrowthPerMonthInMB > maximumMonthlyGrowth)
                {
                    lowestStakeToMonthlyIncomeRatio = stakeToMonthlyIncomeRatio;
                    producerTypeToCandidate = (int)ProducerTypeEnum.History;
                }
            }

            if (_providerConfigurations.AutomaticProduction.ValidatorNode.IsActive && sidechain.ValidatorProducers.RequiredNumberOfProducers > 0)
            {
                var stakeToMonthlyIncomeRatio = GetStakeToMonthlyIncomeRatio(stakeToPut, contractInfo.MinPaymentPerBlockValidatorProducers, contractInfo.MaxPaymentPerBlockValidatorProducers, (int)contractInfo.BlockTimeDuration);
                if (lowestStakeToMonthlyIncomeRatio >= stakeToMonthlyIncomeRatio &&
                    Math.Round((decimal)contractInfo.MinPaymentPerBlockValidatorProducers / 10000, 4) >= (decimal)_providerConfigurations.AutomaticProduction.ValidatorNode.MinBBTPerBlock &&
                    Convert.ToDecimal(_providerConfigurations.AutomaticProduction.ValidatorNode.MaxStakeToMonthlyIncomeRatio) > stakeToMonthlyIncomeRatio)
                {
                    lowestStakeToMonthlyIncomeRatio = stakeToMonthlyIncomeRatio;
                    producerTypeToCandidate = (int)ProducerTypeEnum.Validator;
                }
            }

            if (producerTypeToCandidate == 0) return (false, 0, 0, 0);

            return (true, producerTypeToCandidate, stakeToPut, clientInfo.SidechainCreationTimestamp);
        }

        private decimal GetStakeToMonthlyIncomeRatio(decimal stake, ulong minPaymentPerBlock, ulong maxPaymentPerBlock, int blockTimeDuration)
        {
            var convertedMinPaymentPerBlock = Math.Round((decimal)minPaymentPerBlock / 10000, 4);
            var convertedMaxPaymentPerBlock = Math.Round((decimal)maxPaymentPerBlock / 10000, 4);
            var averagePaymentPerBlock = (convertedMinPaymentPerBlock + convertedMaxPaymentPerBlock) / 2;
            var blocksPerMonth = (decimal)2592000 / blockTimeDuration;
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
                return sidechainContext.SidechainStateManager.TaskContainer.Task.Status == TaskStatus.Running;
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