using System;
using System.Collections.Generic;
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
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
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
            var databasesSemaphores = _concurrentVariables.DatabasesSemaphores;
            var allPendingTransactions = new List<(string statementType, Transaction transaction)>();
            foreach (var sqlCommand in builder.SqlCommands)
            {
                try
                {
                    _transformer.TransformCommand(sqlCommand);
                    builder.BuildSqlStatementsText(_generator, sqlCommand);
                    string sqlTextToExecute = "";
                    if (sqlCommand is DatabaseSqlCommand)
                    {
                        if (_databaseName != null)
                            databasesSemaphores[_databaseName].Release();

                        _databaseName = ((DatabaseSqlCommand)sqlCommand).DatabaseName;

                        if (_databaseName != null)
                        {
                            if (!databasesSemaphores.ContainsKey(_databaseName))
                                databasesSemaphores[_databaseName] = new SemaphoreSlim(1);
                            databasesSemaphores[_databaseName].Wait();
                        }
                    }

                    IList<IList<string>> resultsList;

                    switch (sqlCommand)
                    {
                        case ReadQuerySqlCommand readQuerySql:
                            await SaveAndExecuteExistingTransactions(allPendingTransactions, results, createQueryResult);
                            allPendingTransactions = new List<(string statementType, Transaction transaction)>();
                            var transformedSelectStatement = ((SimpleSelectStatement)readQuerySql.TransformedSqlStatement[0]);
                            var namesAndResults = await ExecuteSelectStatement(
                                builder,
                                (SimpleSelectStatement)readQuerySql.OriginalSqlStatement,
                                transformedSelectStatement,
                                readQuerySql.TransformedSqlStatementText[0]);

                            results.Add(new QueryResult(namesAndResults.ResultRows, namesAndResults.ColumnNames));
                            break;

                        case ChangeRecordSqlCommand changeRecordSqlCommand:
                            sqlTextToExecute = changeRecordSqlCommand.TransformedSqlStatementText[0];
                            // marciak - some updates and deletes require a select statement prior to execution
                            // marciak - this select will be used to identify the actual rows that will be changed
                            if (changeRecordSqlCommand.TransformedSqlStatement[0] is SimpleSelectStatement)
                            {
                                resultsList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                                var finalListOfChanges = _infoPostProcessing.UpdateChangeRecordStatement(changeRecordSqlCommand, resultsList, _databaseName);

                                var changesToExecute = finalListOfChanges.Select(u => u is UpdateRecordStatement up ? _generator.BuildString(up) : _generator.BuildString((DeleteRecordStatement)u)).ToList();

                                foreach (var changeRecordsToExecute in changesToExecute)
                                {
                                    allPendingTransactions.Add((changeRecordSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(changeRecordsToExecute, _databaseName, _nodeConfigurations.ActivePrivateKey)));
                                }
                            }
                            else
                            {
                                allPendingTransactions.Add((changeRecordSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(sqlTextToExecute, _databaseName, _nodeConfigurations.ActivePrivateKey)));
                            }
                            break;


                        case GenericSqlCommand genericSqlCommand:
                            for (int i = 0; i < genericSqlCommand.TransformedSqlStatement.Count; i++)
                            {
                                sqlTextToExecute = genericSqlCommand.TransformedSqlStatementText[i];

                                allPendingTransactions.Add((genericSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(sqlTextToExecute, _databaseName, _nodeConfigurations.ActivePrivateKey)));
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
                                    allPendingTransactions.Add((databaseSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(sqlTextToExecute, "", _nodeConfigurations.ActivePrivateKey)));
                                else
                                    allPendingTransactions.Add((databaseSqlCommand.OriginalSqlStatement.GetStatementType(), CreateTransaction(sqlTextToExecute, _databaseName, _nodeConfigurations.ActivePrivateKey)));
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

            if (_databaseName != null)
                databasesSemaphores[_databaseName].Release();

            return results;
        }

        private async Task SaveAndExecuteExistingTransactions(List<(string statementType, Transaction transaction)> transactions, IList<QueryResult> results, CreateQueryResultDelegate createQueryResult)
        {
            if (transactions.Any())
            {
                await AddPendingTransactions(transactions.Select(p => p.transaction).ToList());
                try
                {
                    _logger.LogDebug($"Executing transactions #{transactions.First().transaction.SequenceNumber} to # {transactions.Last().transaction.SequenceNumber}");
                    await TryToExecuteTransactions(transactions, results, createQueryResult);
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

        //marciak - tries to execute queries if succeeds moves transactions to executed else removes the transactions
        private async Task<OpResult> TryToExecutePendingTransaction(Transaction pendingTransaction)
        {
            var transactionDB = new TransactionDB().TransactionDBFromTransaction(pendingTransaction);
            try
            {
                if (transactionDB.DatabaseName != "") //marciak - distinguishing between server connection and database connection
                    await _connector.ExecuteCommandWithTransactionNumber(transactionDB.TransactionJson, transactionDB.DatabaseName, Convert.ToUInt64(transactionDB.SequenceNumber));
                else
                    await _connector.ExecuteCommand(transactionDB.TransactionJson, transactionDB.DatabaseName);

                await _mongoDbRequesterService.MovePendingTransactionToExecutedAsync(_nodeConfigurations.AccountName, transactionDB);
                _transactionsManager.AddScriptTransactionToSend(transactionDB.TransactionFromTransactionDB());
                return new OpResult(true);
            }
            catch (Exception e)
            {
                await _mongoDbRequesterService.RemovePendingExecutionTransactionAsync(_nodeConfigurations.AccountName, transactionDB);
                var rollback = _concurrentVariables.RollbackTransactionNumber();
                _logger.LogDebug($"Rolling back to #{rollback}");
                return new OpResult(false, e);
            }
        }

        private async Task<OpResult> TryToExecutePendingTransactionsInBatch(List<Transaction> pendingTransactions)
        {
            var transactionsToInsertInDb = new List<TransactionDB>();
            foreach (var transaction in pendingTransactions)
            {
                var transactionDB = new TransactionDB().TransactionDBFromTransaction(transaction);
                transactionsToInsertInDb.Add(transactionDB);
            }

            try
            {
                var databaseName = transactionsToInsertInDb.First().DatabaseName;
                if (databaseName != "")
                    await _connector.ExecuteCommandsWithTransactionNumber(transactionsToInsertInDb, databaseName);
                else
                    await _connector.ExecuteCommands(transactionsToInsertInDb.Select(t => t.TransactionJson).ToList(), databaseName);

                await _mongoDbRequesterService.MovePendingTransactionsToExecutedAsync(_nodeConfigurations.AccountName, transactionsToInsertInDb);
                foreach (var transactionToSend in pendingTransactions)
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
                if (pendingTransaction != null && !await _sqlExecutionHelper.HasTransactionBeenExecuted(pendingTransaction))
                    await TryToExecutePendingTransaction(pendingTransaction.TransactionFromTransactionDB());
            }
        }

        private Transaction CreateTransaction(string json, string databaseName, string senderPrivateKey)
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
                DatabaseName = databaseName
            };

            var serializedTransaction = JsonConvert.SerializeObject(transaction);
            var transactionHash = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(serializedTransaction));

            transaction.TransactionHash = transactionHash;
            transaction.Signature = SignatureHelper.SignHash(senderPrivateKey, transactionHash);
            // _logger.LogDebug(transaction.BlockHash.ToString() + ":" + transaction.DatabaseName + ":" + transaction.SequenceNumber + ":" + transaction.Json + ":" + transaction.Signature + ":" + transaction.Timestamp);
            return transaction;
        }

    }
}