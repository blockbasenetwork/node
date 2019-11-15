using BlockBase.Domain.Database;
using BlockBase.Domain.Database.Columns;
using BlockBase.Domain.Database.Operations;
using BlockBase.Domain.Database.Records;
using BlockBase.DataPersistence.Sidechain;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy.Crypto;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wiry.Base32;
using static BlockBase.Utils.Crypto.Onion;

namespace BlockBase.DataProxy
{
    public class ProxyTest
    {
        private const string TEST_DATABASE = "ProducerDb";
        private ILogger _logger;
        private byte[] _masterKey;
        private byte[] _iv;
        private SidechainDatabasesManager _sidechainDatabasesManager;
        private IConnector _connector;
        private SqlOperationsEncryption _converter;
        private static string DATABASE_NAME = "Example";
        private static string PROXY_DATABASE_NAME = "Proxy_Example";

        public ProxyTest(ILogger logger)
        {     
            _logger = logger;
            _connector = new MySqlConnector("localhost", "root", 3306, "qwerty123", _logger);
            _sidechainDatabasesManager = new SidechainDatabasesManager(_connector);
        }

        private void SetMasterIV()
        {
            if(_masterKey == null){
                KeyManager manager = new KeyManager();
                manager.Setup();
                _masterKey = manager.MasterKey;
                _iv = manager.MasterIV;
                _converter = new SqlOperationsEncryption(_masterKey, _iv, _connector, PROXY_DATABASE_NAME);
            }
        }

        public void TestCrypto()
        {
            Console.WriteLine("AES Test...");
            string plainText = "Blockbase";
            SecureRandom random = new SecureRandom();
            var key = new byte[32];
            var iv = new byte[16];
            random.NextBytes(key);
            random.NextBytes(iv);
            var ciphertext = AES256.EncryptWithCBC(Encoding.ASCII.GetBytes(plainText), key, iv);
            Console.WriteLine(Encoding.ASCII.GetString(ciphertext));
            var newPlainText = AES256.DecryptWithCBC(ciphertext, key, iv);
            Console.WriteLine(Encoding.ASCII.GetString(newPlainText));

        }
        //public void TestOnion()
        //{
        //Console.WriteLine("Onion Test.....");
        //string plainText = "Encriptar isto sff";
        //var a = new Onion();
        //var masterKey = new byte[32];
        //var baseKey = new byte[32];
        //var iv = new byte[16];
        //SecureRandom random = new SecureRandom();
        //random.NextBytes(masterKey);
        //random.NextBytes(iv);
        //random.NextBytes(baseKey);
        //byte[] randomIV;
        //var pointP  =JoinEC.GetRandomPoint();

        //byte[] cipherText = a.CreateOnion(Encoding.ASCII.GetBytes(plainText), masterKey, iv, "Users", "Id", out randomIV);
        //var detLayer = a.DecryptRandomLayer(cipherText, masterKey, "Users", "Id", randomIV);
        //var plain = a.DecryptDeterministicLayer(detLayer, masterKey, iv, "Users", "Id");
        //Console.WriteLine(Encoding.ASCII.GetString(plain));

        //var EncryptedJoinToken = a.CreateJoinOnion(Encoding.ASCII.GetBytes(plainText), masterKey, iv, "Users", "Id", pointP, baseKey, randomIV);
        //var joinToken = a.GetJoinToken(EncryptedJoinToken, masterKey, iv, "Users", "Id", randomIV);

        //byte[] cipherText2 = a.CreateOnion(Encoding.ASCII.GetBytes(plainText), masterKey, iv, "People", "Id", out randomIV);
        //var detLayer2 = a.DecryptRandomLayer(cipherText2, masterKey, "People", "Id", randomIV);
        //var EncryptedJoinToken2 = a.CreateJoinOnion(Encoding.ASCII.GetBytes(plainText), masterKey, iv, "People", "Id", pointP, baseKey, randomIV);
        //var joinToken2 = a.GetJoinToken(EncryptedJoinToken2, masterKey, iv, "People", "Id", randomIV);
        //var delta = JoinEC.GetDeltaKey(JoinEC.GetJoinKey(masterKey, "Users", "Id"), JoinEC.GetJoinKey(masterKey, "People", "Id"));
        //var adjustedPoint = JoinEC.Adjust(delta, joinToken);

        //if (adjustedPoint.SequenceEqual(joinToken2)) Console.WriteLine("Sucesso");
        //else Console.WriteLine("Erro");
        //}

        public void TestJoinEC()
        {
            Console.WriteLine("EC TEST.............");
            string plainText = "JoinEC test";
            var a = new Onion();
            var basekey = new byte[32];
            var masterkey = new byte[32];
            var iv = new byte[16];
            SecureRandom random = new SecureRandom();
            random.NextBytes(basekey);
            random.NextBytes(masterkey);
            random.NextBytes(iv);

            var pointP = JoinEC.GetRandomPoint();
            var joinEC = new JoinEC(basekey, masterkey, "Users", "Id", Encoding.ASCII.GetBytes(plainText), pointP);
            var result = joinEC.Encrypt();
            var joinEC2 = new JoinEC(basekey, masterkey, "People", "Id", Encoding.ASCII.GetBytes(plainText), pointP);
            var result2 = joinEC2.Encrypt();
            var delta = JoinEC.GetDeltaKey(joinEC.Key, joinEC2.Key);
            var adjustedPoint = JoinEC.Adjust(delta, result);

            if (adjustedPoint.SequenceEqual(result2)) Console.WriteLine("Sucesso");
            else Console.WriteLine("Erro");
        }

        // public void TestEncryptionSqlOperation()
        // {
        //    var basekey = new byte[32];
        //    var masterkey = new byte[32];
        //    var iv = new byte[16];
        //    SecureRandom random = new SecureRandom();
        //    random.NextBytes(basekey);
        //    random.NextBytes(masterkey);
        //    random.NextBytes(iv);

        //    var encryption = new EncryptSqlOperation(masterkey, iv, "Example");
        //   // var converter = new SqlConverter(masterkey, iv, "Example");
        //    var deleteRecord = new DeleteRecordOperation
        //    {
        //        ColumnName = "Names",
        //        TableName = "Users",
        //        Value = "Fernando"
        //    };

        //    var columns = new List<Column>
        //    {
        //        new PrimaryColumn("Id", false),
        //        new NormalColumn("Names", SqlDbType.VarBinary, false, 500, 10)
        //    };
        //    var table = new Table("Users", columns);
        //    CreateTableOperation createTable = new CreateTableOperation { Table = table };
        //    DeleteColumnOperation deleteColumn = new DeleteColumnOperation { ColumnName = "Age", TableName = "Users" };

        //    InsertRecordOperation insertRecord = new InsertRecordOperation { TableName = "Users" };
        //    Record record1 = new GuidRecord { Column = "Id", Value = Guid.NewGuid() };
        //    Record record2 = new StringRecord { Column = "Names", Value = "Simon" };
        //    Record record3 = new IntRecord { Column = "Age", Value = 22 };
        //    List<Record> records = new List<Record> { record1, record2, record3 };
        //    insertRecord.ValuesToInsert = records;
        //    var updateOperation = new UpdateRecordOperation
        //    {
        //        IdentifierColumn = "Names",
        //        IdentifierValue = "Simon",
        //        TableName = "Users"
        //    };
        //    records = new List<Record> { new StringRecord("Names", "Fernando", 10) };
        //    updateOperation.ValuesToUpdate = records;
        //    var createColumnOperation = new CreateColumnOperation();
        //    var NormalColumn = new NormalColumn("Age", SqlDbType.VarBinary, false, 500, 10);
        //    createColumnOperation.Column = NormalColumn;
        //    createColumnOperation.TableName = "Users";

        //    //var encryptedDeleteRecordOperation = encryption.EncryptDeleteRecord(deleteRecord);

        //    var encryptedCreateOperation = encryption.EncryptCreateTable(createTable);
        //    var encryptedDeleteOperations = encryption.EncryptDeleteColumn(deleteColumn);

        //    var encryptedAddColumnOperation = encryption.EncryptCreateColumn(createColumnOperation);
        //    //var encryptedUpdateOperation = encryption.EncryptUpdateRecord(updateOperation, 0);
        //    Connector connector = new SQLServerConnector("DESKTOP-65TCSDT", "Example");
        //    SidechainDatabaseManager databaseManager = new SidechainDatabaseManager(connector);

        //    foreach (ISqlOperation operations in encryptedCreateOperation) databaseManager.Enqueue(operations);
        //    foreach (ISqlOperation operations in encryptedAddColumnOperation) databaseManager.Enqueue(operations);

        //    databaseManager.Execute(0);
        //    var encryptedInsertOperation = converter.InsertRecord(insertRecord);
        //    databaseManager.Enqueue(encryptedInsertOperation);

        //foreach (DeleteColumn operations in encryptedDeleteOperations) databaseManager.Enqueue(operations);

        //databaseManager.Enqueue(encryptedUpdateOperation);
        //databaseManager.Build(encryptedDeleteRecordOperation);
        //    databaseManager.Execute(0);
        //    foreach (UpdateRecordOperation a in converter.UpdateRecords(updateOperation)) databaseManager.Enqueue(a);

        //    databaseManager.Execute(0);
        //    foreach (DeleteRecordOperation a in converter.DeleteRecord(deleteRecord)) databaseManager.Enqueue(a);
        //    var querybuilder = new QueryBuilder();
        //    querybuilder.Select("Names", "Users");
        //    querybuilder.Select("Id", "Users");
        //    querybuilder.Where("Users", "Names", "Fernando");
        //    converter.QueryOperation(querybuilder);
        //    databaseManager.Execute(0);
        // }

        public void TestQuery()
        {
            SetMasterIV();
            Console.WriteLine("Starting WhereHigher Test");
            var querybuilder = new QueryBuilder();
            querybuilder.Select("Names", "Users");
            querybuilder.Select("Age", "Users");
            querybuilder.WhereHigherThan("Users", "Age", 20);
            var queryAuxiliarData = _converter.QueryOperation(querybuilder);
           
            _logger.LogDebug("Query encrypted: " + queryAuxiliarData.EncryptedQuery);
            int numberOfRows = _connector.QueryDBGetValues(queryAuxiliarData.EncryptedQuery, queryAuxiliarData.Values, DATABASE_NAME);

            var result = _converter.DecryptQueryResult(numberOfRows, querybuilder, queryAuxiliarData);
            PrintResult(result);
           
        }

        private void PrintResult(List<StringQueryResult> result)
        {
            var columnNames = "";
            foreach(var value in result)
            {
                columnNames += value.TableName + "." + value.ColumnName + " ";
            }
            _logger.LogDebug(columnNames);

            for(int i = 0; i < result[0].Values.Count(); i++)
            {
                var lineResult = "";
                for(int j = 0; j < result.Count(); j++)
                {
                    lineResult += result[j].Values[i] + " ";
                }
                _logger.LogDebug(lineResult);
            }
        }

        public async Task CreateTestDatabase()
        {
            SetMasterIV();
            await _sidechainDatabasesManager.CreateDatabase(PROXY_DATABASE_NAME);
            await _sidechainDatabasesManager.CreateDatabase(DATABASE_NAME); //TODO: needs to be encrypted
            var columns = new List<Column>
           {
               new PrimaryColumn("Id"),
               new NormalColumn("Names", false, 500, 10)
           };
            var table = new Table("Users", columns);
            CreateTableOperation createTable = new CreateTableOperation { Table = table };
            var encryptedCreateTable = _converter.CreateTable(createTable, out List<ISqlOperation> bucketInfoOperations);

            _sidechainDatabasesManager.Enqueue(encryptedCreateTable, DATABASE_NAME);
            foreach (var operation in bucketInfoOperations) _sidechainDatabasesManager.Enqueue(operation, PROXY_DATABASE_NAME);
            await _sidechainDatabasesManager.Execute();

            CreateColumnOperation createColumn = new CreateColumnOperation { TableName = "Users", Column = new RangeColumn("Age", false, 500, 10, 100, false) };
            var encryptCreateColumn = _converter.CreateColumn(createColumn, out bucketInfoOperations);

            foreach (var operation in encryptCreateColumn) _sidechainDatabasesManager.Enqueue(operation, DATABASE_NAME);
            foreach (var operation in bucketInfoOperations) _sidechainDatabasesManager.Enqueue(operation, PROXY_DATABASE_NAME);
            await _sidechainDatabasesManager.Execute();

            InsertRecordOperation insertRecord = new InsertRecordOperation { TableName = "Users" };
            Record record1 = new GuidRecord { Column = "Id", Value = Guid.NewGuid() };
            Record record2 = new StringRecord { Column = "Names", Value = "Simon" };
            Record record3 = new IntRecord { Column = "Age", Value = 22 };
            List<Record> records = new List<Record> { record1, record2, record3 };
            insertRecord.ValuesToInsert = records;
            var encryptedRecord = _converter.InsertRecord(insertRecord);
            _sidechainDatabasesManager.Enqueue(encryptedRecord, DATABASE_NAME);
            await _sidechainDatabasesManager.Execute();




            //    foreach (ISqlOperation operations in encryptedCreateOperation) databaseManager.Enqueue(operations);
            //    foreach (ISqlOperation operations in encryptedAddColumnOperation) databaseManager.Enqueue(operations)

            // var converter = new SqlConverter(_masterKey, _iv, "Example");
            // InsertRecordOperation insertRecord = new InsertRecordOperation { TableName = "Users" };
            // Record record1 = new GuidRecord { Column = "Id", Value = Guid.NewGuid() };
            // Record record2 = new StringRecord { Column = "Names", Value = "Simon" };
            // Record record3 = new IntRecord { Column = "Age", Value = 22 };
            // List<Record> records = new List<Record> { record1, record2, record3 };
            // insertRecord.ValuesToInsert = records;
            // var encryptedInsertOperation = converter.InsertRecord(insertRecord);
            // databaseManager.Enqueue(encryptedInsertOperation);

        }

        //     if (queryResult[0].Values.Count() != 30) Console.WriteLine("Something went wrong!");
        //     else Console.WriteLine("Success");

        // Console.WriteLine("Starting WhereHigher Test"); //WhereHigher
        // Console.WriteLine("Result: ");
        // querybuilder = new QueryBuilder();
        // querybuilder.Select("Names", "Users");
        // querybuilder.Select("Value", "Users").WhereHigherThan("Users", "Age", -20);
        // queryResult = converter.QueryOperation(querybuilder);

        // if (queryResult[0].Values.Count() != 19) Console.WriteLine("Something went wrong!");
        // else Console.WriteLine("Success");

        // Console.WriteLine("Starting WhereHigherOrEqual Test"); //WhereHigherOrEqual
        // Console.WriteLine("Result: ");
        // querybuilder = new QueryBuilder();
        // querybuilder.Select("Names", "Users");
        // querybuilder.Select("Value", "Users").WhereHigherOrEqual("Users", "Age", -20);
        // queryResult = converter.QueryOperation(querybuilder);

        // if (queryResult[0].Values.Count() != 20) Console.WriteLine("Something went wrong!");
        // else Console.WriteLine("Success");

        // Console.WriteLine("Starting Join Test"); //Join
        // Console.WriteLine("Result: ");
        // querybuilder = new QueryBuilder();
        // querybuilder.Select("Names", "Users");
        // querybuilder.Select("Age", "Users");
        // querybuilder.Select("Value", "Users").Join("Employee", "UserId", "Users", "Id").WhereLessThan("Users", "Age", 0);
        // queryResult = converter.QueryOperation(querybuilder);

        // if (queryResult[0].Values.Count() != 1) Console.WriteLine("Something went wrong!");
        // else Console.WriteLine("Success");

        // Console.WriteLine("Starting Join multiple tables Test"); //Join multiple tables
        // Console.WriteLine("Result: ");
        // querybuilder = new QueryBuilder();
        // querybuilder.Select("Names", "Users");
        // querybuilder.Select("Age", "Users");
        // querybuilder.Select("Value", "Users").Select("Text", "Posts")
        //     .Join("Employee", "UserId", "Users", "Id").Join("Posts", "UserId", "Users", "Id").WhereLessThan("Users", "Age", 0);
        // queryResult = converter.QueryOperation(querybuilder);

        // if (queryResult[0].Values.Count() != 20) Console.WriteLine("Something went wrong!");
        // else Console.WriteLine("Success");

        //     Console.WriteLine("Starting Equal Queries on Normal tables Test"); //Equality queries on Normal tables
        //     Console.WriteLine("Result: ");
        //     querybuilder = new QueryBuilder();
        //     querybuilder.Select("Names", "Users");
        //     querybuilder.Select("Age", "Users");
        //     querybuilder.Select("Value", "Users").Select("Text", "Posts")
        //         .Join("Employee", "UserId", "Users", "Id").Join("Posts", "UserId", "Users", "Id").Where("Posts", "Text", "Hello1");
        //     queryResult = converter.QueryOperation(querybuilder);

        //     if (queryResult[0].Values.Count() != 1) Console.WriteLine("Something went wrong!");
        //     else Console.WriteLine("Success");

        //     Console.WriteLine("Starting Where Equals on RangeColumns Test"); //Where Equals on RangeColumns
        //     Console.WriteLine("Result: ");
        //     querybuilder = new QueryBuilder();
        //     querybuilder.Select("Names", "Users");
        //     querybuilder.Select("Value", "Users").Where("Users", "Age", -19);
        //     queryResult = converter.QueryOperation(querybuilder);

        //     if (queryResult[0].Values.Count() != 1) Console.WriteLine("Something went wrong!");
        //     else Console.WriteLine("Success");

        //     Console.WriteLine("Starting Get Database Structure test"); //Get Database Structure
        //     Console.WriteLine("Result: ");
        //     var structure = converter.GetDatabaseStructure();
        //     var it = structure.GetEnumerator();
        //     while (it.MoveNext())
        //     {
        //         var value = it.Current;
        //         Console.WriteLine("Table: " + value.Key);
        //         Console.WriteLine("Columns: ");
        //         var it2 = value.Value.GetEnumerator();
        //         while (it2.MoveNext())
        //         {
        //             var column = it2.Current;
        //             Console.WriteLine(" - " + column.Item1 + "(" + column.Item2 + ")");
        //         }
        //     }
        // }
        //public async Task PopulateMongoDB()
        //{
        // IConnector connector = new MySQLConnector("localhost", "root", "Example", 3306, "qwerty123");
        // var databaseManager = new SidechainDatabaseManager(connector);
        // IMongoDbProducerService service = new MongoDbProducerService();
        // KeyManager manager = new KeyManager();
        // manager.Setup();
        // byte[] masterkey = manager.MasterKey;
        // byte[] iv = manager.MasterIV;
        // var mysqlConnector = new Connectors.MySQLConnector("localhost", "root", "Example", 3306, "qwerty123");
        // var converter = new SqlConverter(masterkey, iv, mysqlConnector, "Example");
        // //Users Table
        // var columns = new List<Column>
        // {
        //     new PrimaryColumn("Id"),
        //     new NormalColumn("Names",false, 500, 10),
        //     new RangeColumn("Value", false, 500, 20, 120)
        // };
        // var table = new Table("Users", columns);
        // CreateTableOperation createTable = new CreateTableOperation { Table = table };
        // //Employee Table
        // var columns2 = new List<Column>
        // {
        //     new PrimaryColumn("Id"),
        //     new RangeColumn("Salary", false, 500, 20, 100000),
        //     new ForeignColumn("UserId",true,"Users", "Id")
        // };
        // var table2 = new Table("Employee", columns2);
        // CreateTableOperation createTable2 = new CreateTableOperation { Table = table2 };
        // //TODO: check why SQLDBTYPE
        // //Post Table
        // var columns3 = new List<Column>
        // {
        //     new PrimaryColumn("Id"),
        //     new NormalColumn("Text", true, 500, 20),
        //     new ForeignColumn("UserId",true,"Users", "Id")
        // };
        // var table3 = new Table("Posts", columns3);
        // CreateTableOperation createTable3 = new CreateTableOperation { Table = table3 };

        // var encryptedCreateOperation = converter.CreateTable(createTable);
        // var encryptedCreateOperation2 = converter.CreateTable(createTable2);
        // var encryptedCreateOperation3 = converter.CreateTable(createTable3);
        // foreach (ISqlOperation sql in encryptedCreateOperation)
        // {
        //     databaseManager.Enqueue(sql);
        //     if (sql is CreateTableOperation)
        //     {
        //         var transaction = new TransactionDB
        //         {
        //             Blockhash = "5c9bb0b3164d1971bfe1389b",
        //             Id = new MongoDB.Bson.ObjectId(),
        //             TransactionJson = JsonConvert.SerializeObject(sql),
        //             TransactionType = "CreateTable"
        //         };

        //         await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE,transaction);
        //     }
        //     else
        //     {
        //         var transaction = new TransactionDB
        //         {
        //             Blockhash = "5c9bb0b3164d1971bfe1389b",
        //             Id = new MongoDB.Bson.ObjectId(),
        //             TransactionJson = JsonConvert.SerializeObject(sql),
        //             TransactionType = "InsertRecord"
        //         };
        //         service = new MongoDbProducerService();
        //         await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction);
        //     }
        // }
        // foreach (ISqlOperation sql in encryptedCreateOperation2)
        // {
        //     databaseManager.Enqueue(sql);
        //     if (sql is CreateTableOperation)
        //     {
        //         var transaction = new TransactionDB
        //         {
        //             Blockhash ="5c9bb0b3164d1971bfe1389b",
        //             Id = new MongoDB.Bson.ObjectId(),
        //             TransactionJson = JsonConvert.SerializeObject(sql),
        //             TransactionType = "CreateTable"
        //         };
        //         service = new MongoDbProducerService();
        //         await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction);
        //     }
        //     else
        //     {
        //         var transaction = new TransactionDB
        //         {
        //             Blockhash = "5c9bb0b3164d1971bfe1389b",
        //             Id = new MongoDB.Bson.ObjectId(),
        //             TransactionJson = JsonConvert.SerializeObject(sql),
        //             TransactionType = "InsertRecord"
        //         };
        //         service = new MongoDbProducerService();
        //         await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction);
        //     }
        // }
        // foreach (ISqlOperation sql in encryptedCreateOperation3)
        // {
        //     databaseManager.Enqueue(sql);
        //     if (sql is CreateTableOperation)
        //     {
        //         var transaction = new TransactionDB
        //         {
        //             Blockhash = "5c9bb0b3164d1971bfe1389b",
        //             Id = new MongoDB.Bson.ObjectId(),
        //             TransactionJson = JsonConvert.SerializeObject(sql),
        //             TransactionType = "CreateTable"
        //         };
        //         await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction);
        //     }
        //     else
        //     {
        //         var transaction = new TransactionDB
        //         {
        //             Blockhash = "5c9bb0b3164d1971bfe1389b",
        //             Id = new MongoDB.Bson.ObjectId(),
        //             TransactionJson = JsonConvert.SerializeObject(sql),
        //             TransactionType = "InsertRecord"
        //         };
        //         await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction);
        //     }
        // }
        // databaseManager.Execute(0);
        // var createColumnOperation = new CreateColumnOperation
        // {
        //     Column = new RangeColumn("Age", false, 500, 20, 100, true),
        //     TableName = "Users"
        // };
        // var createColumn = converter.CreateColumn(createColumnOperation);
        // foreach (ISqlOperation sql in createColumn)
        // {
        //     databaseManager.Enqueue(sql);
        //     if (sql is CreateColumnOperation)
        //     {
        //         var transaction = new TransactionDB
        //         {
        //             Blockhash = "5c9bb0b3164d1971bfe1389b",
        //             Id = new MongoDB.Bson.ObjectId(),
        //             TransactionJson = JsonConvert.SerializeObject(sql),
        //             TransactionType = "CreateColumn"
        //         };
        //         await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction);
        //     }
        //     else
        //     {
        //         var transaction = new TransactionDB
        //         {
        //             Blockhash = "5c9bb0b3164d1971bfe1389b",
        //             Id = new MongoDB.Bson.ObjectId(),
        //             TransactionJson = JsonConvert.SerializeObject(sql),
        //             TransactionType = "InsertRecord"
        //         };
        //         await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction);
        //     }
        // }
        // databaseManager.Execute(0);
        // Guid guid = Guid.NewGuid();
        // for (int i = 0; i < 50; i++)
        // {
        //     InsertRecordOperation insertRecord = new InsertRecordOperation { TableName = "Users" };
        //     guid = Guid.NewGuid();
        //     Record record11 = new GuidRecord { Column = "Id", Value = guid };
        //     Record record12;
        //     record12 = new StringRecord { Column = "Names", Value = i.ToString() };
        //     var age = -50 + i;
        //     Record record13 = new IntRecord { Column = "Age", Value = age };
        //     Record record14 = new IntRecord { Column = "Value", Value = i };
        //     List<Record> records;

        //     if (i == 10) records = new List<Record> { record11, record13, record14 };
        //     else records = new List<Record> { record11, record12, record13, record14 };

        //     insertRecord.ValuesToInsert = records;
        //     var encryptedInsert = converter.InsertRecord(insertRecord);
        //     databaseManager.Enqueue(encryptedInsert);
        //     var transaction = new TransactionDB
        //     {
        //         Blockhash = "5c9bb0b3164d1971bfe1389b",
        //         Id = new MongoDB.Bson.ObjectId(),
        //         TransactionJson = JsonConvert.SerializeObject(encryptedInsert),
        //         TransactionType = "InsertRecord"
        //     };
        //     await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction);
        // }

        // databaseManager.Execute(0);

        // Record record21 = new GuidRecord { Column = "Id", Value = Guid.NewGuid() };
        // Record record22 = new IntRecord { Column = "Salary", Value = 3213 };
        // Record record23 = new GuidRecord { Column = "UserId", Value = guid };

        // List<Record> records2 = new List<Record> { record21, record22, record23 };

        // InsertRecordOperation insertRecord2 = new InsertRecordOperation { TableName = "Employee", ValuesToInsert = records2 };

        // for (int i = 0; i < 20; i++)
        // {
        //     Record record31 = new GuidRecord { Column = "Id", Value = Guid.NewGuid() };
        //     Record record32 = new StringRecord { Column = "Text", Value = "Hello" + i};
        //     Record record33 = new GuidRecord { Column = "UserId", Value = guid };
        //     List<Record> records3 = new List<Record> { record31, record32, record33 };
        //     InsertRecordOperation insertRecord3 = new InsertRecordOperation { TableName = "Posts", ValuesToInsert = records3 };
        //     var encryptedInsert3 = converter.InsertRecord(insertRecord3);
        //     databaseManager.Enqueue(encryptedInsert3);
        //     var transaction3 = new TransactionDB
        //     {
        //         Blockhash = "5c9bb0b3164d1971bfe1389b",
        //         Id = new MongoDB.Bson.ObjectId(),
        //         TransactionJson = JsonConvert.SerializeObject(encryptedInsert3),
        //         TransactionType = "InsertRecord"
        //     };
        //     await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction3);
        // }
        // var encryptedInsert2 = converter.InsertRecord(insertRecord2);
        // databaseManager.Enqueue(encryptedInsert2);
        // var transaction2 = new TransactionDB
        // {
        //     Blockhash = "5c9bb0b3164d1971bfe1389b",
        //     Id = new MongoDB.Bson.ObjectId(),
        //     TransactionJson = JsonConvert.SerializeObject(encryptedInsert2),
        //     TransactionType = "InsertRecord"
        // };
        // await service.AddTransactionToSidechainDatabaseAsync(TEST_DATABASE, transaction2);
        // databaseManager.Execute(0);

        // var updateOperation = new UpdateRecordOperation
        // {
        //     IdentifierColumn = "Names",
        //     IdentifierValue = "1",
        //     TableName = "Users"
        // };
        // var recordsToUpdate = new List<Record> { new StringRecord("Names", "Fernando") };
        // updateOperation.ValuesToUpdate = recordsToUpdate;
        // var operations = converter.UpdateRecords(updateOperation);
        // databaseManager.Execute(0);
        //}
    }
}
