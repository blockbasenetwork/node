
using System;
using System.Collections.Concurrent;
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
using BlockBase.Domain.Database.Sql.SqlCommand;
using BlockBase.Domain.Protos;
using BlockBase.Domain.Results;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Runtime.Network;
using BlockBase.Utils.Crypto;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;

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
        private PeerConnectionsHandler _peerConnectionsHandler;
        private INetworkService _networkService;
        private NodeConfigurations _nodeConfigurations;
        private NetworkConfigurations _networkConfigurations;

        public StatementExecutionManager(Transformer transformer, IGenerator generator, ILogger logger, IConnector connector, InfoPostProcessing infoPostProcessing, ConcurrentVariables databaseAccess, INetworkService networkService, PeerConnectionsHandler peerConnectionsHandler, NetworkConfigurations networkConfigurations, NodeConfigurations nodeConfigurations)
        {
            _transformer = transformer;
            _generator = generator;
            _logger = logger;
            _connector = connector;
            _infoPostProcessing = infoPostProcessing;
            _databaseAccess = databaseAccess;
            _peerConnectionsHandler = peerConnectionsHandler;
            _networkService = networkService;
            _nodeConfigurations = nodeConfigurations;
            _networkConfigurations = networkConfigurations;
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
                            _logger.LogDebug(sqlTextToExecute);
                            resultsList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                            var queryResult = _infoPostProcessing.TranslateSelectResults(readQuerySql, resultsList, _databaseName);
                            results.Add(queryResult);

                            foreach (var title in queryResult.Columns) _logger.LogDebug(title + " ");
                            foreach (var row in queryResult.Data)
                            {
                                Console.WriteLine();
                                foreach (var value in row) _logger.LogDebug(value + " ");
                            }
                            break;

                        case UpdateSqlCommand updateSqlCommand:
                            sqlTextToExecute = updateSqlCommand.TransformedSqlStatementText[0];
                            _logger.LogDebug(sqlTextToExecute);

                            resultsList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                            var finalListOfUpdates = _infoPostProcessing.UpdateUpdateRecordStatement(updateSqlCommand, resultsList, _databaseName);

                            var updatesToExecute = finalListOfUpdates.Select(u => _generator.BuildString(u)).ToList();

                            foreach (var updateToExecute in updatesToExecute)
                            {
                                _logger.LogDebug(updateToExecute);                                                            
                                await _connector.ExecuteCommand(updateToExecute, _databaseName);
                                await SendTransactionToProducers(updateToExecute, _databaseName); 

                            }
                            results.Add(createQueryResult(true, updateSqlCommand.OriginalSqlStatement.GetStatementType()));
                            break;

                        case DeleteSqlCommand deleteSqlCommand:
                            sqlTextToExecute = deleteSqlCommand.TransformedSqlStatementText[0];
                            _logger.LogDebug(sqlTextToExecute);

                            resultsList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                            var finalListOfDeletes = _infoPostProcessing.UpdateDeleteRecordStatement(deleteSqlCommand, resultsList, _databaseName);

                            var deletesToExecute = finalListOfDeletes.Select(u => _generator.BuildString(u)).ToList();

                            foreach (var deleteToExecute in deletesToExecute)
                            {
                                _logger.LogDebug(deleteToExecute);
                                await _connector.ExecuteCommand(deleteToExecute, _databaseName);
                                await SendTransactionToProducers(deleteToExecute, _databaseName);               
                            }
                            results.Add(createQueryResult(true, deleteSqlCommand.OriginalSqlStatement.GetStatementType()));
                            break;


                        case GenericSqlCommand genericSqlCommand:
                            for (int i = 0; i < genericSqlCommand.TransformedSqlStatement.Count; i++)
                            {
                                sqlTextToExecute = genericSqlCommand.TransformedSqlStatementText[i];
                                _logger.LogDebug(sqlTextToExecute);                               
                                await _connector.ExecuteCommand(sqlTextToExecute, _databaseName);
                                await SendTransactionToProducers(sqlTextToExecute, _databaseName); 
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
                                
                                _logger.LogDebug(sqlTextToExecute);
                                await SendTransactionToProducers(sqlTextToExecute, _databaseName ?? ""); 
                            }
                            results.Add(createQueryResult(true, databaseSqlCommand.OriginalSqlStatement.GetStatementType()));
                            break;
                        case ListOrDiscoverCurrentDatabaseCommand listOrDiscoverCurrentDatabase:
                            if (listOrDiscoverCurrentDatabase.OriginalSqlStatement is ListDatabasesStatement)
                            {
                                var databasesList = _infoPostProcessing.GetDatabasesList();
                                _logger.LogDebug("Databases:");
                                foreach (var database in databasesList) _logger.LogDebug(database);
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
                    _logger.LogError("Error executing sql command.", e);
                    results.Add(createQueryResult(false, sqlCommand.OriginalSqlStatement.GetStatementType(), e.Message));
                }
            }
            if (_databaseName != null)
                databasesSemaphores[_databaseName].Release();
            return results;
        }

        private async Task SendTransactionToProducers(string queryToExecute, string databaseName)
        {
            var transactionNumber = Convert.ToUInt64(_databaseAccess.GetNextTransactionNumber());
            foreach(var peerConnection in _peerConnectionsHandler.CurrentPeerConnections)
            {
                var transaction = CreateTransaction(queryToExecute, transactionNumber, databaseName, _nodeConfigurations.ActivePrivateKey);                
                var message = new NetworkMessage(NetworkMessageTypeEnum.SendTransaction, TransactionProtoToMessageData(transaction.ConvertToProto(), _nodeConfigurations.AccountName), TransportTypeEnum.Tcp, _nodeConfigurations.ActivePrivateKey, _nodeConfigurations.ActivePublicKey, _networkConfigurations.LocalIpAddress+":"+_networkConfigurations.LocalTcpPort, peerConnection.ConnectionAccountName, peerConnection.IPEndPoint);
                await _networkService.SendMessageAsync(message);
            }
        }

         private Transaction CreateTransaction(string json, ulong sequenceNumber, string databaseName, string senderPrivateKey)
        {
            var transaction =  new Transaction()
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

        private byte[] TransactionProtoToMessageData(TransactionProto transactionProto, string sidechainName)
        {
            var transactionBytes = transactionProto.ToByteArray();
            // logger.LogDebug($"Block Bytes {HashHelper.ByteArrayToFormattedHexaString(blockBytes)}");

            var sidechainNameBytes = Encoding.UTF8.GetBytes(sidechainName);
            // logger.LogDebug($"Sidechain Name Bytes {HashHelper.ByteArrayToFormattedHexaString(sidechainNameBytes)}");

            short lenght = (short) sidechainNameBytes.Length;
            // logger.LogDebug($"Lenght {lenght}");

            var lengthBytes = BitConverter.GetBytes(lenght);
            // logger.LogDebug($"Lenght Bytes {HashHelper.ByteArrayToFormattedHexaString(lengthBytes)}");

            var data = lengthBytes.Concat(sidechainNameBytes).Concat(transactionBytes).ToArray();
            // logger.LogDebug($"Data {HashHelper.ByteArrayToFormattedHexaString(data)}");

            return data;
        }
    }
}