using System;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using BlockBase.Node.Commands.Utils;
using BlockBase.Runtime.Requester;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using System.Linq;
using EosSharp.Core.Exceptions;
using BlockBase.Domain.Eos;
using BlockBase.Runtime.Sql;

namespace BlockBase.Node.Commands.Requester
{
    public class EndSidechainCommand : AbstractCommand
    {

        private ILogger _logger;
        private ISidechainMaintainerManager _sidechainMaintainerManager;
        private NodeConfigurations _nodeConfigurations;
        private IMainchainService _mainchainService;
        private ConcurrentVariables _concurrentVariables;
         


        public override string CommandName => "End sidechain";

        public override string CommandInfo => "Permantly stops sidechain";

        public override string CommandUsage => "end sidechain";

        public EndSidechainCommand(ILogger logger, ISidechainMaintainerManager sidechainMaintainerManager, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, ConcurrentVariables concurrentVariables)
        {
            _logger = logger;
            _sidechainMaintainerManager = sidechainMaintainerManager;
            _nodeConfigurations = nodeConfigurations;
            _mainchainService = mainchainService;
            _concurrentVariables = concurrentVariables;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                await _sidechainMaintainerManager.End();

                //TODO rpinto - should all this functionality below be encapsulated inside the sidechainMaintainerManager?
                var contractSt = await _mainchainService.RetrieveContractState(_nodeConfigurations.AccountName);
                if (contractSt == null) return new CommandExecutionResponse( HttpStatusCode.BadRequest, new OperationResponse(false, $"Sidechain {_nodeConfigurations.AccountName} not found"));

                var account = await _mainchainService.GetAccount(_nodeConfigurations.AccountName);
                var verifyBlockPermission = account.permissions.Where(p => p.perm_name == EosMsigConstants.VERIFY_BLOCK_PERMISSION).FirstOrDefault();
                var verifyHistoryPermisson = account.permissions.Where(p => p.perm_name == EosMsigConstants.VERIFY_HISTORY_PERMISSION).FirstOrDefault();
                
                if (verifyBlockPermission != null)
                {
                    try
                    {
                        await _mainchainService.UnlinkAction(_nodeConfigurations.AccountName, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
                    }
                    catch (ApiErrorException) 
                    {
                        _logger.LogDebug($"Unlink failed because link does not exist");
                    }
                    await _mainchainService.DeletePermission(_nodeConfigurations.AccountName, EosMsigConstants.VERIFY_BLOCK_PERMISSION);
                }

                if (verifyHistoryPermisson != null)
                {
                    try
                    {
                        await _mainchainService.UnlinkAction(_nodeConfigurations.AccountName, EosMethodNames.HISTORY_VALIDATE);
                    }
                    catch (ApiErrorException) 
                    {
                        _logger.LogDebug($"Unlink failed because link does not exist");
                    }
                    await _mainchainService.DeletePermission(_nodeConfigurations.AccountName, EosMsigConstants.VERIFY_HISTORY_PERMISSION);
                }
            
                var tx = await _mainchainService.EndChain(_nodeConfigurations.AccountName);

                _concurrentVariables.Reset();

                return new CommandExecutionResponse( HttpStatusCode.OK, new OperationResponse(true, $"Ended sidechain. Tx: {tx}"));
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
            return commandStr.ToLower().StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            return new CommandParseResult(true, true);
        }

    }
}