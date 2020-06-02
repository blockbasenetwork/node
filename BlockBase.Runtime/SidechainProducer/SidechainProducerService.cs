﻿using BlockBase.Domain.Configurations;
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
            _endpoint = _networkConfigurations.PublicIpAddress + ":" + _networkConfigurations.TcpPort;
        }

        public async Task Run(bool RecoverChains = true)
        {
            if (RecoverChains) await GetSidechainsFromRecoverDB();
        }

        public void AddSidechainToProducerAndStartIt(SidechainPool sidechain)
        {
            try
            {
                var sidechainManager = new SidechainManager(sidechain, _peerConnectionsHandler, _nodeConfigurations, _networkConfigurations, _endpoint, _logger, _networkService, _mongoDbProducerService, _blockSender, _mainchainService);
                var sidechainAdded = _sidechainKeeper.TryAddSidechain(sidechain);
                if(!sidechainAdded) throw new Exception($"Unable to add sidechain {sidechain.ClientAccountName} to sidechain keeper");

                var task = sidechainManager.Start();
                sidechain.ManagerTask = task;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to add sidechain. Exception thrown: {ex.Message}");
                throw ex;
            }
        }

        public void RemoveSidechainFromProducerAndStopIt(SidechainPool sidechain)
        {
            try
            {
                var sidechainRemoved = _sidechainKeeper.TryRemoveSidechain(sidechain);
                if(!sidechainRemoved) throw new Exception($"Unable to remove sidechain {sidechain.ClientAccountName} from sidechain keeper");
                //TODO rpinto - should the stop be before trying to remove it from the keeper?
                sidechain.ManagerTask.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to remove sidechain. Exception thrown: {ex.Message}");
                throw ex;
            }
        }
        
        public async Task GetSidechainsFromRecoverDB()
        {
            var sidechainsDB = await _mongoDbProducerService.GetAllProducingSidechainsAsync();
            foreach(var sidechainDB in sidechainsDB)
            {
                var contractState = await _mainchainService.RetrieveContractState(sidechainDB.Id);
                if (contractState == null || (!contractState.ProductionTime && !contractState.CandidatureTime)) continue;

                var producersInChain = await _mainchainService.RetrieveProducersFromTable(sidechainDB.Id);
                if (!producersInChain.Any(p => p.Key == _nodeConfigurations.AccountName)) continue;

                var candidatesInChain = await _mainchainService.RetrieveCandidates(sidechainDB.Id);
                if (candidatesInChain.Any(p => p.Key == _nodeConfigurations.AccountName)) continue;

                var sidechainPool = new SidechainPool(sidechainDB.Id);
                AddSidechainToProducerAndStartIt(sidechainPool);
            }
        }

        public Dictionary<string, SidechainPool> GetSidechains() => _sidechainKeeper.Sidechains.ToDictionary(d => d.Key, d => d.Value);

    }
}
