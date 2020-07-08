using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime;
using BlockBase.DataPersistence.Data;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Database.QueryParser;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.QueryParser;
using BlockBase.Domain.Database.Sql.SqlCommand;
using BlockBase.Domain.Results;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Provider;
using BlockBase.Utils.Crypto;
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
        private BareBonesSqlBaseVisitor<object> _visitor;

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
            _visitor = new BareBonesSqlVisitor();
        }

        public delegate QueryResult CreateQueryResultDelegate(bool success, string statementType, string exceptionMessage = null);

        public async Task<IList<QueryResult>> ExecuteSqlText(string sqlString, CreateQueryResultDelegate createQueryResult)
        {
            var builder = ParseSqlText(sqlString);
            return await ExecuteBuilder(builder, createQueryResult);
        }

        public async Task<IList<QueryResult>> ExecuteBuilder(Builder builder, CreateQueryResultDelegate createQueryResult)
        {

            var results = new List<QueryResult>();
            var databasesSemaphores = _concurrentVariables.DatabasesSemaphores;
            foreach (var sqlCommand in builder.SqlCommands)
            {
                var pendingTransactions = new List<Transaction>();
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
                            //_logger.LogDebug(sqlTextToExecute);
                            if (changeRecordSqlCommand.TransformedSqlStatement[0] is SimpleSelectStatement)
                            {
                                resultsList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                                var finalListOfUpdates = _infoPostProcessing.UpdateChangeRecordStatement(changeRecordSqlCommand, resultsList, _databaseName);

                                var changesToExecute = finalListOfUpdates.Select(u => u is UpdateRecordStatement up ? _generator.BuildString(up) : _generator.BuildString((DeleteRecordStatement)u)).ToList();

                                foreach (var changeRecordsToExecute in changesToExecute)
                                {
                                    //_logger.LogDebug(changeRecordsToExecute);
                                    pendingTransactions.Add(CreateTransaction(changeRecordsToExecute, _databaseName, _nodeConfigurations.ActivePrivateKey));
                                }
                            }
                            else
                            {
                                pendingTransactions.Add(CreateTransaction(sqlTextToExecute, _databaseName, _nodeConfigurations.ActivePrivateKey));
                            }
                            results.Add(createQueryResult(true, changeRecordSqlCommand.OriginalSqlStatement.GetStatementType()));
                            break;


                        case GenericSqlCommand genericSqlCommand:
                            for (int i = 0; i < genericSqlCommand.TransformedSqlStatement.Count; i++)
                            {
                                sqlTextToExecute = genericSqlCommand.TransformedSqlStatementText[i];
                                //_logger.LogDebug(sqlTextToExecute);

                                pendingTransactions.Add(CreateTransaction(sqlTextToExecute, _databaseName, _nodeConfigurations.ActivePrivateKey));
                            }
                            results.Add(createQueryResult(true, genericSqlCommand.OriginalSqlStatement.GetStatementType()));
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
                                    pendingTransactions.Add(CreateTransaction(sqlTextToExecute, "", _nodeConfigurations.ActivePrivateKey));
                                else
                                    pendingTransactions.Add(CreateTransaction(sqlTextToExecute, _databaseName, _nodeConfigurations.ActivePrivateKey));
                            }
                            results.Add(createQueryResult(true, databaseSqlCommand.OriginalSqlStatement.GetStatementType()));
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
                            _logger.LogDebug("if statement");
                            break;
                    }

                    await AddPendingTransactions(pendingTransactions);

                    var orderedTransactions = pendingTransactions.OrderBy(t => t.SequenceNumber);
                    foreach (var transaction in orderedTransactions)
                        await TryToExecutePendingTransaction(transaction);

                }
                catch (Exception e)
                {
                    _logger.LogError($"Error executing sql command.{e}");
                    results.Add(createQueryResult(false, sqlCommand.OriginalSqlStatement.GetStatementType(), e.Message));
                }
            }
            if (_databaseName != null)
                databasesSemaphores[_databaseName].Release();

            return results;
        }

        private async Task<ResultsAndColumnNamesPoco> ExecuteSelectStatement(Builder builder, SimpleSelectStatement originalSimpleSelectStatement, SimpleSelectStatement transformedSimpleSelectStatement, string sqlTextToExecute)
        {
            var extraParsingNotNeeded = transformedSimpleSelectStatement.Offset.HasValue;
            //_logger.LogDebug(sqlTextToExecute);
            int missingNumberOfRows;
            List<IList<string>> resultRows = new List<IList<string>>();
            IList<string> columnNames;
            while (true)
            {
                var resultsList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                missingNumberOfRows = _infoPostProcessing.TranslateSelectResults(originalSimpleSelectStatement, transformedSimpleSelectStatement, resultsList, _databaseName, resultRows, out columnNames, extraParsingNotNeeded);

                if (resultsList.Count() == 0 || missingNumberOfRows == 0)
                    break;

                transformedSimpleSelectStatement.Limit = missingNumberOfRows;
                transformedSimpleSelectStatement.Offset = resultsList.Count();
                sqlTextToExecute = builder.BuildSimpleSelectStatementString(transformedSimpleSelectStatement, _generator);
            }
            return new ResultsAndColumnNamesPoco(columnNames, resultRows);

        }

        private async Task AddPendingTransactions(IList<Transaction> transactions)
        {
            var transactionsDB = transactions.Select(t => new TransactionDB().TransactionDBFromTransaction(t)).ToList();
            await _mongoDbRequesterService.AddPendingExecutionTransactionsAsync(_nodeConfigurations.AccountName, transactionsDB);
        }

        private async Task TryToExecutePendingTransaction(Transaction transaction)
        {
            var transactionDB = new TransactionDB().TransactionDBFromTransaction(transaction);
            try
            {
                if (transactionDB.DatabaseName != "")
                    await _connector.ExecuteCommandWithTransactionNumber(transactionDB.TransactionJson, transactionDB.DatabaseName, transactionDB.SequenceNumber);
                else
                    await _connector.ExecuteCommand(transactionDB.TransactionJson, transactionDB.DatabaseName);

                await _mongoDbRequesterService.MovePendingTransactionToExecutedAsync(_nodeConfigurations.AccountName, transactionDB);
                _transactionsManager.AddScriptTransactionToSend(transactionDB.TransactionFromTransactionDB());
            }
            catch
            {
                await _mongoDbRequesterService.RemovePendingExecutionTransactionAsync(_nodeConfigurations.AccountName, transactionDB);
                _concurrentVariables.RollbackOneTransactionNumber();
            }
        }

        private Builder ParseSqlText(string sqlString)
        {
            AntlrInputStream inputStream = new AntlrInputStream(sqlString);
            BareBonesSqlLexer lexer = new BareBonesSqlLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            BareBonesSqlParser parser = new BareBonesSqlParser(commonTokenStream);
            var context = parser.sql_stmt_list();
            return (Builder)_visitor.Visit(context);
        }

        public async Task LoadAndExecutePendingTransaction()
        {
            var pendingTransactions = await _mongoDbRequesterService.RetrievePendingTransactions(_nodeConfigurations.AccountName);
            foreach (var pendingTransaction in pendingTransactions)
            {
                if (pendingTransaction != null && !await HasTransactionBeenExecuted(pendingTransaction))
                    await TryToExecutePendingTransaction(pendingTransaction.TransactionFromTransactionDB());
            }
        }

        private async Task<bool> HasTransactionBeenExecuted(TransactionDB pendingTransaction)
        {
            _databaseName = pendingTransaction.DatabaseName;
            var builder = ParseSqlText(pendingTransaction.TransactionJson);
            var sqlStatement = builder.SqlCommands[0].OriginalSqlStatement;

            var createDatabaseStatement = sqlStatement as CreateDatabaseStatement;
            var dropDatabaseStatement = sqlStatement as DropDatabaseStatement;

            if (createDatabaseStatement != null)
                return await _connector.DoesDatabaseExist(createDatabaseStatement.DatabaseName.Value);

            if (dropDatabaseStatement != null)
                return !await _connector.DoesDatabaseExist(dropDatabaseStatement.DatabaseName.Value);


            return await _connector.WasTransactionExecuted(pendingTransaction.DatabaseName, pendingTransaction.SequenceNumber);
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