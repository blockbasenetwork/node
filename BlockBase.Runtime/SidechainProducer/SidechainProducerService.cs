using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Network.Sidechain;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Runtime.SidechainProducer
{
    public class SidechainProducerService : ISidechainProducerService
    {
        //TODO: Catch events when sidechain service task has stopped and remove sidechain from sidechain services and sidechain keeper

        private readonly SidechainKeeper _sidechainKeeper;
        private readonly BlockValidator _blockValidator;
        private readonly TransactionValidator _transactionValidator;
        private readonly PeerConnectionsHandler _peerConnectionsHandler;
        private readonly NetworkConfigurations _networkConfigurations;
        private readonly NodeConfigurations _nodeConfigurations;
        private INetworkService _networkService;
        private IMainchainService _mainchainService;
        private IMongoDbProducerService _mongoDbProducerService;
        private string _endpoint;
        private ILogger _logger;
        private BlockSender _blockSender;

        public SidechainProducerService(SidechainKeeper sidechainKeeper, PeerConnectionsHandler peerConnectionsHandler, IOptions<NodeConfigurations> nodeConfigurations, IOptions<NetworkConfigurations> networkConfigurations, ILogger<SidechainProducerService> logger, INetworkService networkService,
                                        IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, BlockValidator blockValidator, TransactionValidator transactionValidator, BlockSender blockSender)
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
            _endpoint = _networkConfigurations.LocalIpAddress + ":" + _networkConfigurations.LocalTcpPort;
        }

        public async Task Run(bool RecoverChains = true)
        {
            if (RecoverChains) await GetSidechainsFromRecoverDB();
        }

        public bool AddSidechainToProducer(SidechainPool sidechain)
        {
            try
            {
                var sidechainManager = new SidechainManager(sidechain, _peerConnectionsHandler, _nodeConfigurations, _networkConfigurations, _endpoint, _logger, _networkService, _mongoDbProducerService, _blockSender, _mainchainService);
                var sidechainAdded = _sidechainKeeper.TryAddSidechain(sidechain);

                sidechainManager.Start();
                
                return sidechainAdded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to add sidechain. Exception thrown: {ex.Message}");
                return false;
            }
        }

        public bool RemoveSidechainFromProducer(SidechainPool sidechain)
        {
            try
            {
                return _sidechainKeeper.TryRemoveSidechain(sidechain);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to remove sidechain. Exception thrown: {ex.Message}");
                return false;
            }
        }
        
        public async Task GetSidechainsFromRecoverDB()
        {
            var sidechainsDB = await _mongoDbProducerService.GetAllProducingSidechainsAsync();
            foreach(var sidechainDB in sidechainsDB)
            {
                var sidechainPool = new SidechainPool(sidechainDB.Id);
                AddSidechainToProducer(sidechainPool);
            }
        }

        public Dictionary<string, SidechainPool> GetSidechains() => _sidechainKeeper.Sidechains.ToDictionary(d => d.Key, d => d.Value);

    }
}
