using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.Sql.QueryParser;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sql;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlockBase.Runtime.Requester
{
    public class PendingTransactionRecovery
    {
        private StatementExecutionManager _statementExecutionManager;


        public PendingTransactionRecovery(DatabaseKeyManager databaseKeyManager, ILogger<PendingTransactionRecovery> logger, IConnector connector, ConcurrentVariables concurrentVariables, TransactionsManager transactionSender, IOptions<NodeConfigurations> nodeConfigurations, IMongoDbRequesterService mongoDbRequesterService)
        {
            var middleMan = new MiddleMan(databaseKeyManager);
            var infoPostProcessing = new InfoPostProcessing(middleMan);
            var generator = new PSqlGenerator();
            var transformer = new Transformer(middleMan);
            _statementExecutionManager = new StatementExecutionManager(transformer, generator, logger, connector, infoPostProcessing, concurrentVariables, transactionSender, nodeConfigurations.Value, mongoDbRequesterService);

        }
        public async Task Run()
        {
            await _statementExecutionManager.LoadAndExecutePendingTransaction();
        }
    }
}