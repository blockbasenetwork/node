using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence.Data;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Domain.Enums;
using BlockBase.Domain;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.Runtime.Provider.StateMachine.SidechainState;

namespace BlockBase.Runtime.Provider
{
    public class SidechainProducerService : ISidechainProducerService
    {
        //TODO: Catch events when sidechain service task has stopped and remove sidechain from sidechain services and sidechain keeper

        private readonly SidechainKeeper _sidechainKeeper;
        private readonly BlockValidationsHandler _blockValidator;
        private readonly TransactionValidationsHandler _transactionValidator;
        private readonly PeerConnectionsHandler _peerConnectionsHandler;
        private readonly NetworkConfigurations _networkConfigurations;
        private readonly NodeConfigurations _nodeConfigurations;
        private INetworkService _networkService;
        private IMainchainService _mainchainService;
        private IMongoDbProducerService _mongoDbProducerService;
        private ILogger _logger;
        private BlockRequestsHandler _blockSender;
        private TransactionValidationsHandler _transactionValidationsHandler;


        public SidechainProducerService(SidechainKeeper sidechainKeeper, PeerConnectionsHandler peerConnectionsHandler, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, ILogger<SidechainProducerService> logger, INetworkService networkService,
                                        IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, BlockValidationsHandler blockValidator, TransactionValidationsHandler transactionValidator, BlockRequestsHandler blockSender, TransactionValidationsHandler transactionValidationsHandler)
        {
            _networkService = networkService;
            _mainchainService = mainchainService;
            _logger = logger;
            _sidechainKeeper = sidechainKeeper;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkConfigurations = networkConfigurations?.Value;
            _nodeConfigurations = nodeConfigurations?.Value;
            _mongoDbProducerService = mongoDbProducerService;
            _blockValidator = blockValidator;
            _transactionValidator = transactionValidator;
            _blockSender = blockSender;
            _transactionValidationsHandler = transactionValidationsHandler;
        }

        public async Task Run(bool recoverChains = true)
        {
            if (recoverChains) await LoadAndRunSidechainsFromRecoverDB();
        }

        //TODO rpinto - this probably needs to be in a try catch
        public async Task AddSidechainToProducerAndStartIt(string sidechainName, int producerType = 0, bool automatic = false)
        {

            if (_sidechainKeeper.ContainsKey(sidechainName)) throw new Exception("Sidechain already exists");


            //TODO rpinto - this operation may fail
            var sidechainPool = await FetchSidechainPoolInfoFromSmartContract(sidechainName);
            sidechainPool.ProducerType = producerType != 0 ? (ProducerTypeEnum)producerType : sidechainPool.ProducerType;

            var sidechainStateManager = new SidechainStateManager(sidechainPool, _peerConnectionsHandler, _nodeConfigurations, _networkConfigurations, _logger, _networkService, _mongoDbProducerService, _mainchainService, _blockSender, _transactionValidationsHandler, automatic);

            var sidechainContext = new SidechainContext
            {
                SidechainPool = sidechainPool,
                SidechainStateManager = sidechainStateManager
            };

            var sidechainAdded = _sidechainKeeper.TryAddSidechain(sidechainContext);
            if (!sidechainAdded) throw new Exception($"Unable to add sidechain {sidechainName} to sidechain keeper");

            var task = sidechainStateManager.Start();
            sidechainPool.ManagerTask = task;

        }

        public void RemoveSidechainFromProducerAndStopIt(string sidechainName)
        {

            if (!_sidechainKeeper.ContainsKey(sidechainName)) throw new Exception($"Sidechain not found");
            var sidechainRemoved = _sidechainKeeper.TryRemoveSidechain(sidechainName, out var sidechainContext);
            if (!sidechainRemoved) throw new Exception($"Unable to remove sidechain {sidechainName} from sidechain keeper");
            //TODO rpinto - should the stop be before trying to remove it from the keeper?

            sidechainContext.SidechainStateManager.Stop();

        }

        public async Task LoadAndRunSidechainsFromRecoverDB()
        {
            var sidechainsDB = await _mongoDbProducerService.GetAllProducingSidechainsAsync();
            foreach (var sidechainDB in sidechainsDB)
            {
                //TODO rpinto - why are these checks here?? doesn't make any sense
                // var contractState = await _mainchainService.RetrieveContractState(sidechainDB.Id);
                // if (contractState == null || (!contractState.ProductionTime && !contractState.CandidatureTime)) continue;

                // var producersInChain = await _mainchainService.RetrieveProducersFromTable(sidechainDB.Id);
                // if (!producersInChain.Any(p => p.Key == _nodeConfigurations.AccountName)) continue;

                // var candidatesInChain = await _mainchainService.RetrieveCandidates(sidechainDB.Id);
                // if (candidatesInChain.Any(p => p.Key == _nodeConfigurations.AccountName)) continue;

                try
                {
                    await AddSidechainToProducerAndStartIt(sidechainDB.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unable to recover {sidechainDB.Id}", ex.Message);
                }
            }
        }

        public bool DoesChainExist(string sidechainName)
        {
            return _sidechainKeeper.ContainsKey(sidechainName);
        }

        IEnumerable<SidechainContext> ISidechainProducerService.GetSidechainContexts()
        {
            return _sidechainKeeper.GetSidechains();
        }

        public SidechainContext GetSidechainContext(string sidechainName)
        {
            var sidechainContextRetrieved = _sidechainKeeper.TryGet(sidechainName, out var sidechainContext);
            if(!sidechainContextRetrieved) throw new Exception($"Unable to retrieve {sidechainName}");
            return sidechainContext;
        }

        private async Task<SidechainPool> FetchSidechainPoolInfoFromSmartContract(string sidechainName)
        {
            var sidechainPool = new SidechainPool();

            var publicKey = (await _mainchainService.RetrieveClientTable(sidechainName)).PublicKey;

            sidechainPool.ClientAccountName = sidechainName;
            sidechainPool.ClientPublicKey = publicKey;

            var contractInformation = await _mainchainService.RetrieveContractInformation(sidechainName);
            var candidatesInTable = await _mainchainService.RetrieveCandidates(sidechainName);
            var producersInTable = await _mainchainService.RetrieveProducersFromTable(sidechainName);

            sidechainPool.BlockTimeDuration = contractInformation.BlockTimeDuration;
            sidechainPool.BlocksBetweenSettlement = contractInformation.BlocksBetweenSettlement;
            sidechainPool.BlockSizeInBytes = contractInformation.SizeOfBlockInBytes;

            var selfCandidate = candidatesInTable.Where(p => p.Key == _nodeConfigurations.AccountName).SingleOrDefault();
            var selfProducer = producersInTable.Where(p => p.Key == _nodeConfigurations.AccountName).SingleOrDefault();


            if (selfProducer != null)
            {
                sidechainPool.ProducerType = (ProducerTypeEnum)selfProducer.ProducerType;

                var producersInPool = producersInTable.Select(m => new ProducerInPool
                {
                    ProducerInfo = new ProducerInfo
                    {
                        AccountName = m.Key,
                        PublicKey = m.PublicKey,
                        ProducerType = (ProducerTypeEnum)m.ProducerType,
                        NewlyJoined = true
                    }
                }).ToList();

                sidechainPool.ProducersInPool.ClearAndAddRange(producersInPool);
            }
            else if (selfCandidate != null)
            {
                sidechainPool.ProducerType = (ProducerTypeEnum)selfCandidate.ProducerType;
                sidechainPool.CandidatureOnStandby = true;
            }

            return sidechainPool;
        }
    }
}
