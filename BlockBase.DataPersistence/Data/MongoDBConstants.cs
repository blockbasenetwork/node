namespace BlockBase.DataPersistence.Data.MongoDbEntities
{
    public class MongoDbConstants
    {
        public static readonly string CONNECTION_STRING = "mongodb://localhost";
        public static readonly string RECOVER_DATABASE_NAME = "recoverDb";
        public static readonly string BLOCKHEADERS_COLLECTION_NAME = "Blockheaders";
        public static readonly string LAST_SEARCHED_FOR_TRANSACTIONS_BLOCKHEADER_COLLECTION_NAME = "LastSearchedForTransactionsBlockHeader";
        public static readonly string PROVIDER_TRANSACTIONS_COLLECTION_NAME = "Transactions";
        public static readonly string REQUESTER_PENDING_EXECUTION_TRANSACTIONS_COLLECTION_NAME = "PendingExecutionTransactions";
        public static readonly string REQUESTER_TRANSACTIONS_COLLECTION_NAME = "TransactionsToSend";
        public static readonly string PROVIDER_CURRENT_TRANSACTION_TO_EXECUTE_COLLECTION_NAME = "CurrentTransactionToExecute";
        public static readonly string PRODUCING_SIDECHAINS_COLLECTION_NAME = "ProducingSidechains";
        public static readonly string MAINTAINED_SIDECHAINS_COLLECTION_NAME = "MaintainedSidechains";
        public const string CREATE_DATABASE = "CreateDatabase";
        public const string CREATE_TABLE = "CreateTable";
        public const string CREATE_COLUMN = "CreateColumn";
        public const string DELETE_COLUMN = "DeleteColumn";
        public const string DELETE_RECORD = "DeleteRecord";
        public const string DELETE_TABLE = "DeleteTable";
        public const string INSERT_RECORD = "InsertRecord";
        public const string UPDATE_RECORD = "UpdateRecord";

    }

}