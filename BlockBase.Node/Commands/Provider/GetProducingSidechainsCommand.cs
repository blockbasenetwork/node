using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Enums;
using BlockBase.Domain.Eos;
using BlockBase.Domain.Results;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Provider;
using Microsoft.Extensions.Logging;

namespace BlockBase.Node.Commands.Provider
{
    public class GetProducingSidechainsCommand : AbstractCommand
    {
        private ISidechainProducerService _sidechainProducerService;
        private NodeConfigurations _nodeConfigurations;
        private IMainchainService _mainchainService;

        private ILogger _logger;


        public override string CommandName => "Get producing sidechains";

        public override string CommandInfo => "Gets current producing sidechains";

        public override string CommandUsage => "get chains";

        public GetProducingSidechainsCommand(ILogger logger, ISidechainProducerService sidechainProducerService, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _sidechainProducerService = sidechainProducerService;
            _mainchainService = mainchainService;
            _logger = logger;
            _nodeConfigurations = nodeConfigurations;
        }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                var poolOfSidechains = _sidechainProducerService.GetSidechainContexts();
                var producingSidechainsResult = new List<ProducingSidechain>();

                foreach (var sidechain in poolOfSidechains)
                {
                    var warnings = await _mainchainService.RetrieveWarningTable(sidechain.SidechainPool.ClientAccountName);
                    var blocksCount = await _mainchainService.RetrieveBlockCount(sidechain.SidechainPool.ClientAccountName);
                    var candidates = await _mainchainService.RetrieveCandidates(sidechain.SidechainPool.ClientAccountName);
                    var producers = await _mainchainService.RetrieveProducersFromTable(sidechain.SidechainPool.ClientAccountName);
                    var state = await _mainchainService.RetrieveContractState(sidechain.SidechainPool.ClientAccountName);
                    var providerState = candidates.Any(c => c.Key == _nodeConfigurations.AccountName) ? "Candidate" :
                                        producers.Any(p => p.Key == _nodeConfigurations.AccountName) ? "Producer" :
                                        "Provider not found in sidechain candidates or producers";                  
                        var producingSidechain = new ProducingSidechain()
                        {
                            Name = sidechain.SidechainPool.ClientAccountName,
                            ProviderState = providerState,
                            SidechainState = GetSidechainState(state),
                            BlocksProducedInCurrentSettlement = Convert.ToInt32(blocksCount.Where(b => b.Key == _nodeConfigurations.AccountName).SingleOrDefault()?.blocksproduced),
                            BlocksFailedInCurrentSettlement = Convert.ToInt32(blocksCount.Where(b => b.Key == _nodeConfigurations.AccountName).SingleOrDefault()?.blocksfailed)
                        };
                        foreach (var warning in warnings.Where(w => w.Producer == _nodeConfigurations.AccountName))
                        {
                            producingSidechain.Warnings.Add(new ProducingSidechainWarning()
                            {
                                WarningType = (WarningTypeEnum)warning.WarningType,
                                WarningTimestamp = DateTimeOffset.FromUnixTimeSeconds((long)warning.WarningCreationDateInSeconds).DateTime
                            });
                        }
                        producingSidechainsResult.Add(producingSidechain);                    
                }

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<List<ProducingSidechain>>(producingSidechainsResult, $"Get producing sidechains successful."));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 2;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 2) return new CommandParseResult(true, true);

            return new CommandParseResult(true, CommandUsage);
        }

        private string GetSidechainState(BlockBase.Network.Mainchain.Pocos.ContractStateTable states)
        {
            if (states.CandidatureTime)
                return SidechainStatesConstants.CANDIDATURE;
            if (states.ConfigTime)
                return SidechainStatesConstants.CONFIGURATION;
            if (states.IPReceiveTime)
                return SidechainStatesConstants.IP_RECEIVE_TIME;
            if (states.IPSendTime)
                return SidechainStatesConstants.IP_SEND_TIME;
            if (states.SecretTime)
                return SidechainStatesConstants.SECRET_TIME;
            if (states.ProductionTime)
                return SidechainStatesConstants.PRODUCTION_TIME;
            if (states.Startchain)
                return SidechainStatesConstants.STARTING;
            return SidechainStatesConstants.STATE_NOT_FOUND;
        }
    }
}