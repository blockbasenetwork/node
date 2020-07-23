using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;


namespace BlockBase.Node.Commands.Provider
{
    public class GetDecryptedNodeIpsCommand : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private string _chainName;

        private ILogger _logger;


        public override string CommandName => "Get decrypted nodes ips";

        public override string CommandInfo => "Retrieves the ip addresses of the specified sidechain nodes";

        public override string CommandUsage => "get nodes ips --chain <sidechainName>";

        public GetDecryptedNodeIpsCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _logger = logger;
        }

         public GetDecryptedNodeIpsCommand(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, string chainName) : this(logger, mainchainService, nodeConfigurations)
        {
            _chainName = chainName;
        }


        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_chainName)) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, "Please provide a valid account name"));
                _chainName = _chainName.Trim();

                var contractState = await _mainchainService.RetrieveContractState(_chainName);
                var producers = await _mainchainService.RetrieveProducersFromTable(_chainName);
                var contractInfo = await _mainchainService.RetrieveContractInformation(_chainName);
                var ipAddresses = await _mainchainService.RetrieveIPAddresses(_chainName);

                if (contractState == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Contract state not found for {_chainName}"));
                if (producers == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Producer table not found for {_chainName}"));
                if (contractInfo == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"Contract info not found for {_chainName}"));
                if (ipAddresses == null) return new CommandExecutionResponse(HttpStatusCode.BadRequest, new OperationResponse(false, $"IP Addresses table not found for {_chainName}"));

                if (!ipAddresses.Any() || ipAddresses.Any(t => !t.EncryptedIPs.Any()))
                    return new CommandExecutionResponse(HttpStatusCode.Unauthorized, new OperationResponse(false, $"IP Addresses table doesn't have any IPs for {_chainName}"));

                if (!producers.Any(m => m.Key == _nodeConfigurations.AccountName))
                    return new CommandExecutionResponse(HttpStatusCode.NotFound, new OperationResponse(false, $"Producer {_nodeConfigurations.AccountName} not found in producers table for {_chainName}"));

                var ipsToReturn = new Dictionary<string, string>();

                foreach (var ipAddressTable in ipAddresses) ipAddressTable.EncryptedIPs.RemoveAt(ipAddressTable.EncryptedIPs.Count - 1);

                int numberOfIpsToTake = (int)Math.Ceiling(producers.Count() / 4.0);
                var orderedProducersInPool = ListHelper.GetListSortedCountingBackFromIndex(producers, producers.FindIndex(m => m.Key == _nodeConfigurations.AccountName)).Take(numberOfIpsToTake).ToList();

                foreach (var producer in orderedProducersInPool)
                {
                    var producerIndex = orderedProducersInPool.IndexOf(producer);
                    var producerIps = ipAddresses.Where(p => p.Key == producer.Key).FirstOrDefault();

                    var listEncryptedIPEndPoints = producerIps.EncryptedIPs;
                    var encryptedIpEndPoint = listEncryptedIPEndPoints[producerIndex];
                    var producerIp = AssymetricEncryption.DecryptIP(encryptedIpEndPoint, _nodeConfigurations.ActivePrivateKey, producer.PublicKey);
                    ipsToReturn.Add(producer.Key, producerIp.ToString());
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
            return commandData.Length == 5;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.StartsWith("get nodes ips");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 5)
            {
                if (commandData[3] != "--chain") return new CommandParseResult(true, CommandUsage);
                _chainName = commandData[4];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }
    }
}