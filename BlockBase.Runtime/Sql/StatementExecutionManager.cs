using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Transaction;
using BlockBase.Domain.Database.Sql.SqlCommand;
using BlockBase.Domain.Results;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Operation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Runtime.Sql
{
    public class StatementExecutionManager
    {
        private Transformer _transformer;
        private ILogger _logger;
        private IGenerator _generator;
        private string _databaseName;
        private IConnector _connector;
        private InfoPostProcessing _infoPostProcessing;
        private ConcurrentVariables _concurrentVariables;
        private TransactionsManager _transactionsManager;
        private NodeConfigurations _nodeConfigurations;
        private IMongoDbRequesterService _mongoDbRequesterService;
        private SqlExecutionHelper _sqlExecutionHelper;

        public StatementExecutionManager(Transformer transformer, IGenerator generator, ILogger logger, IConnector connector, InfoPostProcessing infoPostProcessing, ConcurrentVariables concurrentVariables, TransactionsManager transactionsManager, NodeConfigurations nodeConfigurations, IMongoDbRequesterService mongoDbRequesterService)
        {
            _transformer = transformer;
            _generator = generator;
            _logger = logger;
            _connector = connector;
            _infoPostProcessing = infoPostProcessing;
            _concurrentVariables = concurrentVariables;
            _nodeConfigurations = nodeConfigurations;
            _transactionsManager = transactionsManager;
            _mongoDbRequesterService = mongoDbRequesterService;
            _sqlExecutionHelper = new SqlExecutionHelper(connector);

        }

        public delegate QueryResult CreateQueryResultDelegate(bool success, string statementType, string exceptionMessage = null);

        public async Task<IList<QueryResult>> ExecuteSqlText(string sqlString, CreateQueryResultDelegate createQueryResult)
        {
            var builder = _sqlExecutionHelper.ParseSqlText(sqlString);
            return await ExecuteBuilder(builder, createQueryResult);
        }

        public async Task<IList<QueryResult>> ExecuteBuilder(Builder builder, CreateQueryResultDelegate createQueryResult)
        {
            var results = new List<QueryResult>();
            var allPendingTransactions = new List<(string statementType, Transaction transaction)>();

            var databaseSemaphore = TryGetAndAddDatabaseSemaphore(_nodeConfigurations.AccountName);
            await databaseSemaphore.WaitAsync();
            try
            {
                foreach (var sqlCommand in builder.SqlCommands)
                {
                    try
                    {
                        _transformer.TransformCommand(sqlCommand);
                        builder.BuildSqlStatementsText(_generator, sqlCommand);
                        string sqlTextToExecute = "";

                        if (sqlCommand is DatabaseSqlCommand)
                        {
                            _databaseName = ((DatabaseSqlCommand)sqlCommand).DatabaseName;
                        }

                        IList<IList<string>> resultsList;

                        switch (sqlCommand)
                        {
                            
                            case ReadQuerySqlCommand readQuerySql:
                                var transformedSelectStatement = ((SimpleSelectStatement)readQuerySql.TransformedSqlStatement[0]);
                                var simpleSelectStatement = (SimpleSelectStatement)readQuerySql.OriginalSqlStatement;
                                if(simpleSelectStatement.SelectCoreStatement.CaseExpressions.Count != 0){
                                    foreach(var expression in transformedSelectStatement.SelectCoreStatement.CaseExpressions){
                                        var caseExpression = expression as CaseExpression;
                                        simpleSelectStatement.SelectCoreStatement.ResultColumns.Add(caseExpression.ResultColumn);
                                    }
                                }
                                await SaveAndExecuteExistingTransactions(allPendingTransactions, results, createQueryResult);
                                allPendingTransactions = new List<(string statementType, Transaction transaction)>();
                                
                                var namesAndResults = await ExecuteSelectStatement(
                                    builder,
                                    simpleSelectStatement,//(SimpleSelectStatement)readQuerySql.OriginalSqlStatement,
                                    transformedSelectStatement,
                                    readQuerySql.TransformedSqlStatementText[0]);

                                results.Add(new QueryResult(namesAndResults.ResultRows, namesAndResults.ColumnNames));
                                break;
                            case TransactionSqlCommand transactionSqlCommand:
                                var transactionStatement = (TransactionStatement)transactionSqlCommand.OriginalSqlStatement;
                                string[] sqlTextsToExecute = transactionSqlCommand.TransformedSqlStatementText[0].Split(";");
                                var listOfSqlText = sqlTextsToExecute.ToList();
                                listOfSqlText.RemoveAt(listOfSqlText.Count-1);
                                listOfSqlText = listOfSqlText.Select(x => x + ";").ToList();
                                var transactionGroupId = Guid.NewGuid().ToString();
                                allPendingTransactions.Add(("begin", CreateTransaction("BEGIN;", _databaseName, transactionGroupId)));
                                resultsList = new List<IList<string>>();
                                var operationStatementCount = transactionStatement.OperationStatements.Count;
                                for(int i = 0; i < operationStatementCount; i++){
                                    var operation = transactionStatement.OperationStatements[i];
                                    if(operation.GetStatementType() == "insert record"){
                                        allPendingTransactions.Add((operation.GetStatementType(), CreateTransaction(listOfSqlText.ElementAt(i), _databaseName, transactionGroupId)));
                                    } else if(operation.GetStatementType() == "update record"){
                                        var updateOperation = (UpdateRecordStatement)operation;
                                        var newChangeRecordSqlCommand = new ChangeRecordSqlCommand(updateOperation);
                                        newChangeRecordSqlCommand.TransformedSqlStatementText = new List<string>();
                                        newChangeRecordSqlCommand.TransformedSqlStatementText.Add(listOfSqlText.ElementAt(i));
                                        newChangeRecordSqlCommand.TransformedSqlStatement = new List<ISqlStatement>();
                                        newChangeRecordSqlCommand.TransformedSqlStatement.Add(updateOperation);
                                        await ChangeRecordSqlCommandMethod(newChangeRecordSqlCommand,resultsList,allPendingTransactions, transactionGroupId);
                                    } else if(operation.GetStatementType() == "delete record"){
                                        var deleteOperation = (DeleteRecordStatement)operation;
                                        var newChangeRecordSqlCommand = new ChangeRecordSqlCommand(deleteOperation);
                                        newChangeRecordSqlCommand.TransformedSqlStatementText = new List<string>();
                                        newChangeRecordSqlCommand.TransformedSqlStatementText.Add(listOfSqlText.ElementAt(i));
                                        newChangeRecordSqlCommand.TransformedSqlStatement = new List<ISqlStatement>();
                                        newChangeRecordSqlCommand.TransformedSqlStatement.Add(deleteOperation);
                                        await ChangeRecordSqlCommandMethod(newChangeRecordSqlCommand,resultsList,allPendingTransactions, transactionGroupId);
                                    }
                                    
                                }
                                allPendingTransactions.Add(("commit", CreateTransaction("COMMIT;", _databaseName, transactionGroupId)));
                                break;
                            case ChangeRecordSqlCommand changeRecordSqlCommand:
                                
                                resultsList = new List<IList<string>>();
                                await ChangeRecordSqlCommandMethod(changeRecordSqlCommand,resultsList,allPendingTransactions);
                                break;


                            case GenericSqlCommand genericSqlCommand:
                                for (int i = 0; i < genericSqlCommand.TransformedSqlStatement.Count; i++)
                                {
                                    sqlTextToExecute = genericSqlCommand.TransformedSqlStatementText[i];

                                    allPendingTransactions.Add((genericSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(sqlTextToExecute, _databaseName)));
                                }
                                break;

                            case DatabaseSqlCommand databaseSqlCommand:
                                if (databaseSqlCommand.OriginalSqlStatement is UseDatabaseStatement)
                                {
                                    results.Add(createQueryResult(true, databaseSqlCommand.OriginalSqlStatement.GetStatementType()));
                                    continue;
                                }

                                if (databaseSqlCommand.OriginalSqlStatement is CreateDatabaseStatement)
                                    await _connector.InsertToDatabasesTable(((CreateDatabaseStatement)databaseSqlCommand.TransformedSqlStatement[0]).DatabaseName.Value);

                                else if (databaseSqlCommand.OriginalSqlStatement is DropDatabaseStatement)
                                    await _connector.DeleteFromDatabasesTable(((DropDatabaseStatement)databaseSqlCommand.TransformedSqlStatement[0]).DatabaseName.Value);

                                for (int i = 0; i < databaseSqlCommand.TransformedSqlStatement.Count; i++)
                                {
                                    sqlTextToExecute = databaseSqlCommand.TransformedSqlStatementText[i];
                                    if (databaseSqlCommand.TransformedSqlStatement[i] is ISqlDatabaseStatement)
                                        allPendingTransactions.Add((databaseSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(sqlTextToExecute, "")));
                                    else
                                        allPendingTransactions.Add((databaseSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(sqlTextToExecute, _databaseName)));
                                }
                                break;

                            case ListOrDiscoverCurrentDatabaseCommand listOrDiscoverCurrentDatabase:
                                if (listOrDiscoverCurrentDatabase.OriginalSqlStatement is ListDatabasesStatement)
                                {
                                    var databasesList = _infoPostProcessing.GetDatabasesList();
                                    //_logger.LogDebug("Databases:");
                                    //foreach (var database in databasesList) _logger.LogDebug(database);
                                    results.Add(new QueryResult(
                                        new List<IList<string>>(databasesList.Select(d => new List<string>() { d }).ToList()),
                                        new List<string>() { "databases" })
                                    );
                                }

                                else
                                {
                                    var currentDatabase = _infoPostProcessing.DecryptDatabaseName(_databaseName) ?? "none";
                                    results.Add(new QueryResult(
                                        new List<IList<string>>() { new List<string>() { currentDatabase } },
                                        new List<string>() { "current_database" })
                                    );
                                }
                                break;

                            case IfSqlCommand ifSqlCommand:
                                var originalSimpleSelectStatement = ((IfStatement)ifSqlCommand.OriginalSqlStatement).SimpleSelectStatement;
                                var transformedSimpleSelectStatement = ((SimpleSelectStatement)(ifSqlCommand.TransformedSqlStatement[0]));
                                sqlTextToExecute = ifSqlCommand.TransformedSqlStatementText[0];
                                if ((await ExecuteSelectStatement(builder, originalSimpleSelectStatement, transformedSimpleSelectStatement, sqlTextToExecute)).ResultRows.Count != 0)
                                {
                                    results.AddRange(await ExecuteBuilder(((IfStatement)ifSqlCommand.OriginalSqlStatement).Builder, createQueryResult));
                                }
                                else
                                {
                                    results.Add(createQueryResult(false, sqlCommand.OriginalSqlStatement.GetStatementType(), "Condition not fulfilled."));
                                }
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Error analyzing sql command.{e}");
                        results.Add(createQueryResult(false, sqlCommand.OriginalSqlStatement.GetStatementType(), e.Message));
                    }
                }

                await SaveAndExecuteExistingTransactions(allPendingTransactions, results, createQueryResult);
            }
            catch (Exception e)
            {
                _logger.LogDebug($"Exception thrown in statement execution: {e}");
            }
            finally
            {
                databaseSemaphore.Release();
            }

            return results;
        }

        private async Task ChangeRecordSqlCommandMethod(ChangeRecordSqlCommand changeRecordSqlCommand, IList<IList<string>> results, List<(string statementType, Transaction transaction)> allPendingTransactions, string transactionGroupId = null){
            var sqlTextToExecute = changeRecordSqlCommand.TransformedSqlStatementText[0];
            var updateRecordStatement = changeRecordSqlCommand.TransformedSqlStatement[0] as UpdateRecordStatement;
            // marciak - some updates and deletes require a select statement prior to execution
            // marciak - this select will be used to identify the actual rows that will be changed
            if (changeRecordSqlCommand.TransformedSqlStatement[0] is SimpleSelectStatement)
            {
                results = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                var finalListOfChanges = _infoPostProcessing.UpdateChangeRecordStatement(changeRecordSqlCommand, results, _databaseName);

                var changesToExecute = finalListOfChanges.Select(u => u is UpdateRecordStatement up ? _generator.BuildString(up) : _generator.BuildString((DeleteRecordStatement)u)).ToList();

                foreach (var changeRecordsToExecute in changesToExecute)
                {
                    allPendingTransactions.Add((changeRecordSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(changeRecordsToExecute, _databaseName, transactionGroupId)));
                }
            }
            else if(updateRecordStatement != null)// TODO need to decrypt rows in case the update statement is used with a CASE statement
            {
                if(updateRecordStatement.CaseExpressions.Count != 0 && changeRecordSqlCommand.TransformedSqlStatement[0] is SimpleSelectStatement){
                    var transformedUpdateRecordStatement = changeRecordSqlCommand.TransformedSqlStatement[0] as UpdateRecordStatement;
                    results = await _connector.ExecuteQuery(_generator.BuildStringToSimpleSelectStatement(transformedUpdateRecordStatement), _databaseName);
                    var finalListOfChanges = _infoPostProcessing.UpdateChangeRecordStatement(changeRecordSqlCommand, results, _databaseName);
                        var changesToExecute = finalListOfChanges.Select(u => u is UpdateRecordStatement up ? _generator.BuildString(up) : _generator.BuildString((DeleteRecordStatement)u)).ToList();

                    foreach (var changeRecordsToExecute in changesToExecute)
                    {
                        allPendingTransactions.Add((changeRecordSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(changeRecordsToExecute, _databaseName, transactionGroupId)));
                    }
                }
            } 
            else 
            {
                allPendingTransactions.Add((changeRecordSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(sqlTextToExecute, _databaseName, transactionGroupId)));
            }
        }

        private async Task SaveAndExecuteExistingTransactions(List<(string statementType, Transaction transaction)> transactions, IList<QueryResult> results, CreateQueryResultDelegate createQueryResult)
        {
            if (transactions.Any())
            {
                await AddPendingTransactions(transactions.Select(p => p.transaction).ToList());
                try
                {
                    var listOfWrappedTransactions = new List<KeyValuePair<string,List<(string statementType, Transaction transaction)>>?>();

                    KeyValuePair<string, List<(string statementType, Transaction transaction)>>? currentValuePair = null;
                    
                    foreach(var t in transactions){
                        var transaction = t.transaction;
                        if(currentValuePair == null){
                            currentValuePair = new KeyValuePair<string, List<(string statementType, Transaction transaction)>>(transaction.TransactionGroupId, new List<(string statementType, Transaction transaction)>());
                            listOfWrappedTransactions.Add(currentValuePair);
                        }
                        if(transaction.TransactionGroupId == currentValuePair.Value.Key){
                            currentValuePair.Value.Value.Add(t);
                        } else {
                            currentValuePair = new KeyValuePair<string, List<(string statementType, Transaction transaction)>>(transaction.TransactionGroupId, new List<(string statementType, Transaction transaction)>());
                            currentValuePair.Value.Value.Add(t);
                            listOfWrappedTransactions.Add(currentValuePair);
                        }
                        
                    }
                    foreach(var wrappedTransactions in listOfWrappedTransactions){
                        if(wrappedTransactions.Value.Key == null){
                            await TryToExecuteTransactions(wrappedTransactions.Value.Value, results, createQueryResult);
                        } else {
                            await TryToExecuteTransactionsInBatchOnly(wrappedTransactions.Value.Value, results, createQueryResult, wrappedTransactions.Value.Key);
                        }
                    }
                    _logger.LogDebug($"Executing transactions #{transactions.First().transaction.SequenceNumber} to # {transactions.Last().transaction.SequenceNumber}");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error executing sql command.{e}");
                    results.Add(createQueryResult(false, "SQL Query", e.Message));
                }
            }
        }

        private async Task<ResultsAndColumnNamesPoco> ExecuteSelectStatement(Builder builder, SimpleSelectStatement originalSimpleSelectStatement, SimpleSelectStatement transformedSimpleSelectStatement, string sqlTextToExecute)
        {
            //marciak- if the offset is set it means that the columns are not encrypted or are unique, so there's no extra parsing needed
            var extraParsingNotNeeded = transformedSimpleSelectStatement.Offset.HasValue;
            //_logger.LogDebug(sqlTextToExecute);
            int missingNumberOfRows;
            List<IList<string>> resultRows = new List<IList<string>>();
            IList<string> columnNames;
            while (true) //marciak - this while runs until the offset and limit are satisfied
            {
                var resultsList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                missingNumberOfRows = _infoPostProcessing.TranslateSelectResults(originalSimpleSelectStatement, transformedSimpleSelectStatement, resultsList, _databaseName, resultRows, out columnNames, extraParsingNotNeeded);

                if (resultsList.Count() == 0 || missingNumberOfRows == 0) // marciak - no more results or already reached required number of rows
                    break;

                transformedSimpleSelectStatement.Limit = missingNumberOfRows;
                transformedSimpleSelectStatement.Offset = transformedSimpleSelectStatement.Offset.HasValue ? transformedSimpleSelectStatement.Offset + resultsList.Count() : resultsList.Count();
                sqlTextToExecute = builder.BuildSimpleSelectStatementString(transformedSimpleSelectStatement, _generator);
            }
            return new ResultsAndColumnNamesPoco(columnNames, resultRows);

        }

        //marciak - saves the transactions to the database  
        private async Task AddPendingTransactions(IList<Transaction> transactions)
        {
            var transactionsDB = transactions.Select(t => new TransactionDB().TransactionDBFromTransaction(t)).ToList();
            await _mongoDbRequesterService.AddPendingExecutionTransactionsAsync(_nodeConfigurations.AccountName, transactionsDB);
        }

        private async Task TryToExecuteTransactions(IList<(string statementType, Transaction transaction)> pendingTransactions, IList<QueryResult> results, CreateQueryResultDelegate createQueryResult)
        {
            var orderedTransactions = pendingTransactions.OrderBy(t => t.transaction.SequenceNumber).ToList();
            OpResult opResult = null;
            while (orderedTransactions.Any())
            {
                var firstTransaction = orderedTransactions.First();
                var groupedTransactionsByDatabaseName = orderedTransactions.TakeWhile(t => t.transaction.DatabaseName == firstTransaction.transaction.DatabaseName).ToList();
                opResult = await TryToExecutePendingTransactionsInBatch(groupedTransactionsByDatabaseName.Select(t => t.transaction).ToList());
                if (!opResult.Succeeded) break;
                orderedTransactions = orderedTransactions.Except(groupedTransactionsByDatabaseName).ToList();
            }

            //If fails in batch tries to execute one by one
            if (!opResult.Succeeded)
            {
                foreach (var transaction in orderedTransactions)
                {
                    opResult = await TryToExecutePendingTransaction(transaction.transaction);
                    if (!opResult.Succeeded)
                    {
                        var followingTransactions = orderedTransactions.SkipWhile(t => t.transaction.SequenceNumber <= transaction.transaction.SequenceNumber).ToList();
                        followingTransactions.ForEach(t => t.transaction.SequenceNumber -= 1);
                        results.Add(createQueryResult(opResult.Succeeded, transaction.statementType, opResult.Exception.Message));
                    }
                }
            }

            if (opResult.Succeeded) results.Add(createQueryResult(opResult.Succeeded, "SQL Query")); ;
        }

        private async Task<OpResult> TryToExecuteTransactionsInBatchOnly(List<(string statementType, Transaction transaction)> pendingTransactions, IList<QueryResult> results, CreateQueryResultDelegate createQueryResult, string transactionGroupId)
        {
            var orderedTransactions = pendingTransactions.OrderBy(t => t.transaction.SequenceNumber).ToList();
            OpResult opResult = null;
            while (orderedTransactions.Any())
            {
                var firstTransaction = orderedTransactions.First();
                var groupedTransactionsByDatabaseName = orderedTransactions.TakeWhile(t => t.transaction.DatabaseName == firstTransaction.transaction.DatabaseName).ToList();
                opResult = await TryToExecutePendingTransactionsInBatch(groupedTransactionsByDatabaseName.Select(t => t.transaction).ToList());
                if (!opResult.Succeeded) break;
                orderedTransactions = orderedTransactions.Except(groupedTransactionsByDatabaseName).ToList();
            }

            //If fails in batch removes from mongo
            if (!opResult.Succeeded)
            {   
                var failedTransactionList = new List<TransactionDB>();
                foreach(var transaction in orderedTransactions){
                    failedTransactionList.Add(new TransactionDB().TransactionDBFromTransaction(transaction.transaction));
                }
                if(failedTransactionList.Count>0){
                    await _mongoDbRequesterService.RemovePendingExecutionTransactionsAsync(_nodeConfigurations.AccountName, failedTransactionList);
                    var rollback = _concurrentVariables.ReloadTransactionNumber();
                    _logger.LogDebug($"Rolling back from #{rollback}");
                }
                return opResult;
            } else {
                results.Add(createQueryResult(opResult.Succeeded, "SQL Query"));
                return opResult;
            } 
        }

        //marciak - tries to execute queries if succeeds moves transactions to executed else removes the transactions
        private async Task<OpResult> TryToExecutePendingTransaction(Transaction pendingTransaction)
        {
            try
            {
                if (pendingTransaction.DatabaseName != "") //marciak - distinguishing between server connection and database connection
                    await _connector.ExecuteCommandWithTransactionNumber(pendingTransaction.Json, pendingTransaction.DatabaseName, pendingTransaction.SequenceNumber);
                else
                    await _connector.ExecuteCommand(pendingTransaction.Json, pendingTransaction.DatabaseName);

                var completedTransaction = ReHashAndSignTransaction(pendingTransaction);
                var transactionDB = new TransactionDB().TransactionDBFromTransaction(completedTransaction);

                await _mongoDbRequesterService.MovePendingTransactionToExecutedAsync(_nodeConfigurations.AccountName, transactionDB);
                _transactionsManager.AddScriptTransactionToSend(completedTransaction);
                return new OpResult(true);
            }
            catch (Exception e)
            {
                await _mongoDbRequesterService.RemovePendingExecutionTransactionAsync(_nodeConfigurations.AccountName, new TransactionDB().TransactionDBFromTransaction(pendingTransaction));
                var rollback = _concurrentVariables.ReloadTransactionNumber();
                _logger.LogDebug($"Rolling back to #{rollback}");
                return new OpResult(false, e);
            }
        }

        private async Task<OpResult> TryToExecutePendingTransactionsInBatch(List<Transaction> pendingTransactions)
        {
            //TODO - rpinto we need to check what happens when an operation fails and how that is dealt regarding the removal of the transaction from the
            // pending transactions and also its broadcast or not to the providers.
            try
            {
                var databaseName = pendingTransactions.First().DatabaseName;
                if (databaseName != ""){
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    await _connector.ExecuteCommandsWithTransactionNumber(pendingTransactions, databaseName);
                    stopWatch.Stop();
                    // Get the elapsed time as a TimeSpan value.
                    TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value.
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    Console.WriteLine("RunTime " + elapsedTime);
                }
                else{
                    await _connector.ExecuteCommands(pendingTransactions.Select(t => t.Json).ToList(), databaseName);
                }
                    

                var completeTransactions = ReHashAndSignTransactions(pendingTransactions);

                var transactionsInsertedInDb = new List<TransactionDB>();
                foreach (var transaction in completeTransactions)
                {
                    var transactionDB = new TransactionDB().TransactionDBFromTransaction(transaction);
                    transactionsInsertedInDb.Add(transactionDB);
                }

                await _mongoDbRequesterService.MovePendingTransactionsToExecutedAsync(_nodeConfigurations.AccountName, transactionsInsertedInDb);
                foreach (var transactionToSend in completeTransactions)
                {
                    _transactionsManager.AddScriptTransactionToSend(transactionToSend);
                }

                return new OpResult(true);
            }
            catch (Exception e)
            {
                _logger.LogDebug($"Failed to execute transactions in batch. Executing manually. Exception {e}");
                return new OpResult(false, e);
            }
        }

        public async Task LoadAndExecutePendingTransaction()
        {
            var pendingTransactions = await _mongoDbRequesterService.RetrievePendingTransactions(_nodeConfigurations.AccountName);
            foreach (var pendingTransaction in pendingTransactions)
            {
                if (pendingTransaction != null) {
                    if(!await _sqlExecutionHelper.HasTransactionBeenExecuted(pendingTransaction)){
                        await TryToExecutePendingTransaction(pendingTransaction.TransactionFromTransactionDB());
                    } else {
                        await _mongoDbRequesterService.MovePendingTransactionToExecutedAsync(_nodeConfigurations.AccountName, pendingTransaction);
                    }
                }
            }
        }
        

        private Transaction CreateTransaction(string json, string databaseName, string transactionGroupId = null)
        {
            var sequenceNumber = Convert.ToUInt64(_concurrentVariables.GetNextTransactionNumber());

            var transaction = new Transaction()
            {
                Json = json,
                BlockHash = new byte[0],
                SequenceNumber = sequenceNumber,
                Timestamp = (ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                TransactionHash = new byte[0],
                Signature = "",
                DatabaseName = databaseName,
                TransactionGroupId = transactionGroupId
            };

            var serializedTransaction = JsonConvert.SerializeObject(transaction);
            var transactionHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedTransaction));

            transaction.TransactionHash = transactionHash;

            // _logger.LogDebug(transaction.BlockHash.ToString() + ":" + transaction.DatabaseName + ":" + transaction.SequenceNumber + ":" + transaction.Json + ":" + transaction.Signature + ":" + transaction.Timestamp);
            return transaction;
        }

        private Transaction ReHashAndSignTransaction(Transaction transaction)
        {
            transaction.TransactionHash = new byte[0];
            var serializedTransaction = JsonConvert.SerializeObject(transaction);
            var transactionHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedTransaction));

            transaction.TransactionHash = transactionHash;
            transaction.Signature = SignatureHelper.SignHash(_nodeConfigurations.ActivePrivateKey, transactionHash);

            return transaction;
        }

        private IList<Transaction> ReHashAndSignTransactions(IList<Transaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                transaction.TransactionHash = new byte[0];
                var serializedTransaction = JsonConvert.SerializeObject(transaction);
                var transactionHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedTransaction));

                transaction.TransactionHash = transactionHash;
                transaction.Signature = SignatureHelper.SignHash(_nodeConfigurations.ActivePrivateKey, transactionHash);
            }

            return transactions;
        }

        private SemaphoreSlim TryGetAndAddDatabaseSemaphore(string database)
        {
            var semaphoreKeyPair = _concurrentVariables.DatabasesSemaphores.FirstOrDefault(s => s.Key == database);

            var defaultKeyValuePair = default(KeyValuePair<string, SemaphoreSlim>);
            if (semaphoreKeyPair.Equals(defaultKeyValuePair))
            {
                var newSemaphore = new SemaphoreSlim(1, 1);
                _concurrentVariables.DatabasesSemaphores.TryAdd(database, newSemaphore);

                return newSemaphore;
            }

            return semaphoreKeyPair.Value;
        }
    }
}