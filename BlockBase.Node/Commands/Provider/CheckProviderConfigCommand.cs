using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Utils;
using BlockBase.Domain.Configurations;
using BlockBase.Network.Mainchain;
using BlockBase.Node.Commands.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Node.Commands.Provider
{
    public class CheckProviderConfig : AbstractCommand
    {
        private IMainchainService _mainchainService;

        private NodeConfigurations _nodeConfigurations;

        private NetworkConfigurations _networkConfigurations;

        private IConnectionsChecker _connectionsChecker;

        private ILogger _logger;


        public override string CommandName => "Check requester configuration";

        public override string CommandInfo => "Verifies if node configuration is valid";

        public override string CommandUsage => "check config";

        public CheckProviderConfig(ILogger logger, IMainchainService mainchainService, NodeConfigurations nodeConfigurations, NetworkConfigurations networkConfigurations, IConnectionsChecker connectionsChecker)
        {
            _mainchainService = mainchainService;
            _nodeConfigurations = nodeConfigurations;
            _connectionsChecker = connectionsChecker;
            _networkConfigurations = networkConfigurations;
            _logger = logger;
        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {

                bool fetchedExternalUtcTimeReference = false;
                TimeSpan timeDifference = TimeSpan.FromSeconds(0);

                DateTime machineUtcDateTime = DateTime.UtcNow;
                DateTime externalUtcDateTime = DateTime.MinValue;



                try
                {
                    var webClient = new WebClient();
                    var result = webClient.DownloadString(new Uri("http://worldtimeapi.org/api/timezone/Etc/UTC"));
                    machineUtcDateTime = DateTime.UtcNow;

                    if (string.IsNullOrWhiteSpace(result))
                        fetchedExternalUtcTimeReference = false;

                    var obj = new { datetime = string.Empty };

                    var jsonResult = JsonConvert.DeserializeObject(result, obj.GetType());

                    string dateTimeToParse = ((dynamic)jsonResult).datetime;
                    DateTime parsedTime;
                    if (!DateTime.TryParse(dateTimeToParse, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsedTime))
                        fetchedExternalUtcTimeReference = false;

                    fetchedExternalUtcTimeReference = true;
                    externalUtcDateTime = parsedTime;
                    timeDifference = machineUtcDateTime - externalUtcDateTime;
                }
                catch
                {

                }


                string configuredPublicIp = _networkConfigurations.PublicIpAddress.Trim();
                string fetchedPublicIp = null;
                bool fetchedPublicIpSuccessfully = false;
                bool isConfiguredIPEqualToPublicIP = false;


                try
                {
                    var webClient = new WebClient();
                    var result = webClient.DownloadString(new Uri("https://api.ipify.org"));
                    fetchedPublicIpSuccessfully = !string.IsNullOrWhiteSpace(result.Trim());
                    fetchedPublicIp = result.Trim();
                    isConfiguredIPEqualToPublicIP = configuredPublicIp == fetchedPublicIp;
                }
                catch
                {

                }

                var isMongoLive = await _connectionsChecker.IsAbleToConnectToMongoDb();
                var isPostgresLive = await _connectionsChecker.IsAbleToConnectToPostgres();

                var accountName = _nodeConfigurations.AccountName;
                var activePublicKey = _nodeConfigurations.ActivePublicKey;


                bool eosAccountDataFetched = false;
                List<string> currencyBalance = null;
                long cpuUsed = 0;
                long cpuLimit = 0;
                long netUsed = 0;
                long netLimit = 0;
                ulong ramUsed = 0;
                long ramLimit = 0;

                bool activeKeyFoundOnAccount = false;
                bool activeKeyHasEnoughWeight = false;


                try
                {
                    var accountInfo = await _mainchainService.GetAccount(_nodeConfigurations.AccountName);
                    currencyBalance = await _mainchainService.GetCurrencyBalance(_networkConfigurations.BlockBaseTokenContract, _nodeConfigurations.AccountName);

                    eosAccountDataFetched = true;
                    cpuUsed = accountInfo.cpu_limit.used;
                    cpuLimit = accountInfo.cpu_limit.max;
                    netUsed = accountInfo.net_limit.used;
                    netLimit = accountInfo.net_limit.max;
                    ramUsed = accountInfo.ram_usage;
                    ramLimit = accountInfo.ram_quota;

                    var permission = accountInfo.permissions.SingleOrDefault(p => p.perm_name == "active");

                    if (permission != null)
                    {
                        var correspondingActiveKey = permission.required_auth?.keys?.SingleOrDefault(k => k.key == activePublicKey);
                        if (correspondingActiveKey != null)
                            activeKeyFoundOnAccount = true;
                        if (correspondingActiveKey != null && correspondingActiveKey.weight >= permission.required_auth.threshold)
                            activeKeyHasEnoughWeight = true;

                    }


                }
                catch { }


                var tcpPort = _networkConfigurations.TcpPort;

                var mongoDbConnectionString = _nodeConfigurations.MongoDbConnectionString;
                var mongoDbPrefix = _nodeConfigurations.DatabasesPrefix;

                var postgresHost = _nodeConfigurations.PostgresHost;
                var postgresPort = _nodeConfigurations.PostgresPort;
                var postgresUser = _nodeConfigurations.PostgresUser;

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse<dynamic>(
                    new
                    {
                        fetchedExternalUtcTimeReference,
                        machineUtcDateTime,
                        externalUtcDateTime,
                        timeDifference,

                        configuredPublicIp,
                        fetchedPublicIpSuccessfully,
                        fetchedPublicIp,
                        isConfiguredIPEqualToPublicIP,

                        tcpPort,
                        accountName,
                        eosAccountDataFetched,
                        activePublicKey,
                        activeKeyFoundOnAccount,
                        activeKeyHasEnoughWeight,
                        currencyBalance,
                        cpuUsed,
                        cpuLimit,
                        netUsed,
                        netLimit,
                        ramUsed,
                        ramLimit,

                        mongoDbConnectionString,
                        mongoDbPrefix,
                        isMongoLive,
                        postgresHost,
                        postgresPort,
                        postgresUser,
                        isPostgresLive,
                    }
                    , $"Configuration and connection data retrieved."));

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