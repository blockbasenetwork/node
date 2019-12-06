using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Database.Operations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlockBase.DataPersistence.Sidechain
{
    public class SidechainDatabasesManager : ISidechainDatabasesManager
    {
        private Dictionary<string, LinkedList<ISqlOperation>> _commandsToExecute = new Dictionary<string, LinkedList<ISqlOperation>>();
        private IConnector _connector;

        public SidechainDatabasesManager(IConnector connector)
        {
            _connector = connector;
        }

        public void ExecuteBlockTransactions(IList<Transaction> blockTransactions)
        {
            foreach (Transaction transaction in blockTransactions)
            {
                //TODO - this is old code and needs to be replaced with new query builder code
                //switch (transaction.TransactionType)
                //{
                //    case MongoDbConstants.CREATE_DATABASE:
                //        Build<CreateDatabaseOperation>(transaction);
                //        break;
                //    case MongoDbConstants.CREATE_TABLE:
                //        Build<CreateTableOperation>(transaction);
                //        break;
                //    case MongoDbConstants.CREATE_COLUMN:
                //        Build<CreateColumnOperation>(transaction);
                //        break;
                //    case MongoDbConstants.DELETE_COLUMN:
                //        Build<DeleteColumnOperation>(transaction);
                //        break;
                //    case MongoDbConstants.DELETE_RECORD:
                //        Build<DeleteRecordOperation>(transaction);
                //        break;
                //    case MongoDbConstants.DELETE_TABLE:
                //        Build<DeleteTableOperation>(transaction);
                //        break;
                //    case MongoDbConstants.INSERT_RECORD:
                //        Build<InsertRecordOperation>(transaction);
                //        break;
                //    case MongoDbConstants.UPDATE_RECORD:
                //        Build<UpdateRecordOperation>(transaction);
                //        break;
                //}
            }
            Execute();
        }

        public void Build<T>(Transaction transaction) where T : ISqlOperation
        {
            //TODO - this code needs to be replaced with new version of query builder
            //JsonConverter[] converters = { new RecordConverter(), new ColumnConverter() };
            //T UpdateRecordOperation = JsonConvert.DeserializeObject<T>(transaction.Json, new JsonSerializerSettings() { Converters = converters });
            //Enqueue(UpdateRecordOperation, transaction.DatabaseName);
        }

        public bool CheckMetaInfoExist(string databaseName)
        {
            return _connector.CheckIfMetaInfoTableExist(databaseName);
        }

        public void Enqueue(ISqlOperation operation, string databaseName)
        {
            _commandsToExecute.TryAdd(databaseName, new LinkedList<ISqlOperation>());
            _commandsToExecute[databaseName].AddLast(operation);
        }

        public async Task CreateDatabase(string databaseName)
        {
            await _connector.CreateDatabase(databaseName);
        }

        public void CreateMetaInfo(string databaseName)
        {
            _connector.CreateMetaInfo(databaseName);
        }

        public async Task<bool> CheckDatabase(string databaseName)
        {
            return await _connector.CheckDatabase(databaseName);
        }

        public async Task Execute()
        {
            await _connector.Execute(_commandsToExecute);
        }
    }
}