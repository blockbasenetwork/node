using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;


namespace BlockBase.Node.Commands.Requester
{
    public class RequesterGetDecryptedNodeIpsCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private ILogger _logger;


        public override string CommandName => "Get decrypted node ips";

        public override string CommandInfo => "Retrieves provider ip addresses";

        public override string CommandUsage => "get req ips";

        public RequesterGetDecryptedNodeIpsCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                var sidechainName = _nodeConfigurations.AccountName;

                var contractState = await _mainchainService.RetrieveContractState(sidechainName);
                var contractInfo = await _mainchainService.RetrieveContractInformation(sidechainName);
                var ipAddresses = await _mainchainService.RetrieveIPAddresses(sidechainName);

                if (contractState == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Contract state not found for {sidechainName}"));
                if (contractInfo == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Contract info not found for {sidechainName}"));
                if (ipAddresses == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"IP Addresses table not found for {sidechainName}"));

                if (!ipAddresses.Any() || ipAddresses.Any(t => !t.EncryptedIPs.Any()))
                    return new CommandExecutionResponse(HttpStatusCode.Unauthorized, new OperationResponse(false, $"IP Addresses table doesn't have any IPs for {sidechainName}"));

                var ipsToReturn = new Dictionary<string, string>();

                foreach (var ipAddressTable in ipAddresses)
                {
                    var encryptedIp = ipAddressTable.EncryptedIPs?.LastOrDefault();
                    var decryptedIp = AssymetricEncryption.DecryptIP(encryptedIp, _nodeConfigurations.ActivePrivateKey, ipAddressTable.PublicKey);
                    ipsToReturn.Add(ipAddressTable.Key, decryptedIp.ToString());
                }

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<Dictionary<string, string>>(ipsToReturn));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }

        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 3;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith(CommandUsage);
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 3) return new CommandParseResult(true, true);
            return new CommandParseResult(true, CommandUsage);
        }



    }
}