using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.SqlCommand;
using BlockBase.Domain.Results;
using BlockBase.Runtime.Network;
using BlockBase.Runtime.Sidechain;
using BlockBase.Utils.Crypto;
using BlockBase.Utils.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockBase.Runtime
{
    public class StatementExecutionManager
    {
        private Transformer _transformer;
        private ILogger _logger;
        private IGenerator _generator;
        private string _databaseName;
        private IConnector _connector;
        private InfoPostProcessing _infoPostProcessing;
        private ConcurrentVariables _databaseAccess;
        private TransactionSender _transactionSender;
        private PeerConnectionsHandler _peerConnectionsHandler;
        private NodeConfigurations _nodeConfigurations;

        public StatementExecutionManager(Transformer transformer, IGenerator generator, ILogger logger, IConnector connector, InfoPostProcessing infoPostProcessing, ConcurrentVariables databaseAccess, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, NetworkConfigurations networkConfigurations, NodeConfigurations nodeConfigurations)
        {
            _transformer = transformer;
            _generator = generator;
            _logger = logger;
            _connector = connector;
            _infoPostProcessing = infoPostProcessing;
            _databaseAccess = databaseAccess;
            _transactionSender = new TransactionSender(logger, nodeConfigurations, networkService, peerConnectionsHandler, networkConfigurations);
            _peerConnectionsHandler = peerConnectionsHandler;
            _nodeConfigurations = nodeConfigurations;
        }

        public delegate QueryResult CreateQueryResultDelegate(bool success, string statementType, string exceptionMessage = null);
        public async Task<IList<QueryResult>> ExecuteBuilder(Builder builder, CreateQueryResultDelegate createQueryResult)
        {
            var results = new List<QueryResult>();
            var databasesSemaphores = _databaseAccess.DatabasesSemaphores;
            foreach (var sqlCommand in builder.SqlCommands)
            {
                try
                {
                    _transformer.TransformCommand(sqlCommand);
                    builder.BuildSqlStatementsText(_generator, sqlCommand);

                    Console.WriteLine();
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
                            sqlTextToExecute = readQuerySql.TransformedSqlStatementText[0];
                            //_logger.LogDebug(sqlTextToExecute);
                            resultsList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                            var queryResult = _infoPostProcessing.TranslateSelectResults(readQuerySql, resultsList, _databaseName);
                            results.Add(queryResult);

                            //foreach (var title in queryResult.Columns) _logger.LogDebug(title + " ");
                            foreach (var row in queryResult.Data)
                            {
                                Console.WriteLine();
                                //foreach (var value in row) _logger.LogDebug(value + " ");
                            }
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
                                    await _connector.ExecuteCommand(changeRecordsToExecute, _databaseName);
                                    SendTransactionToProducers(changeRecordsToExecute, _databaseName);
                                }
                            }
                            else
                            {
                                await _connector.ExecuteCommand(sqlTextToExecute, _databaseName);
                                SendTransactionToProducers(sqlTextToExecute, _databaseName);
                            }
                            results.Add(createQueryResult(true, changeRecordSqlCommand.OriginalSqlStatement.GetStatementType()));
                            break;


                        case GenericSqlCommand genericSqlCommand:
                            for (int i = 0; i < genericSqlCommand.TransformedSqlStatement.Count; i++)
                            {
                                sqlTextToExecute = genericSqlCommand.TransformedSqlStatementText[i];
                                //_logger.LogDebug(sqlTextToExecute);
                                await _connector.ExecuteCommand(sqlTextToExecute, _databaseName);
                                SendTransactionToProducers(sqlTextToExecute, _databaseName);
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
                                    await _connector.ExecuteCommand(sqlTextToExecute, null);
                                else
                                    await _connector.ExecuteCommand(sqlTextToExecute, _databaseName);

                                //_logger.LogDebug(sqlTextToExecute);
                                SendTransactionToProducers(sqlTextToExecute, _databaseName ?? "");
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
                    }
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

        private void SendTransactionToProducers(string queryToExecute, string databaseName)
        {
            var transactionNumber = Convert.ToUInt64(_databaseAccess.GetNextTransactionNumber());
            var transaction = CreateTransaction(queryToExecute, transactionNumber, databaseName, _nodeConfigurations.ActivePrivateKey);
            
            foreach (var peerConnection in _peerConnectionsHandler.CurrentPeerConnections)
                _transactionSender.AddWaitingTransactionToProducer(peerConnection, transaction);
            
            if(_transactionSender.TaskContainer == null || _transactionSender.TaskContainer.Task.IsCompleted)
                _transactionSender.Start();
        }

        private Transaction CreateTransaction(string json, ulong sequenceNumber, string databaseName, string senderPrivateKey)
        {
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
            _logger.LogDebug(transaction.BlockHash.ToString() + ":" + transaction.DatabaseName + ":" + transaction.SequenceNumber + ":" + transaction.Json + ":" + transaction.Signature + ":" + transaction.Timestamp);
            return transaction;
        }
    }
}