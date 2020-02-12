using System;
using BlockBase.Domain.Database.Sql.SqlCommand;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using System.Linq;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.QueryParser;
using BlockBase.Domain.Database.Sql.QueryParser;
using BlockBase.DataPersistence.Sidechain.Connectors;
using Antlr4.Runtime;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BlockBase.DataProxy;
using System.Collections.Generic;
using BlockBase.Domain.Results;
using BlockBase.Domain.Pocos;

namespace BlockBase.Runtime
{
    public class SqlCommandManager
    {
        private Transformer _transformer;
        private IGenerator _generator;
        private string _databaseName = "";
        private InfoPostProcessing _infoPostProcessing;
        private BareBonesSqlBaseVisitor<object> _visitor;
        private IConnector _connector;
        private ILogger _logger;

        // public SqlCommandManager(MiddleMan middleMan, ILogger logger, IConnector connector, INetworkService networkService)
        public SqlCommandManager(MiddleMan middleMan, ILogger logger, IConnector connector)
        {
            _visitor = new BareBonesSqlVisitor();
            _infoPostProcessing = new InfoPostProcessing(middleMan);
            _generator = new PSqlGenerator();
            _logger = logger;
            _connector = connector;
            _transformer = new Transformer(middleMan);
        }

        public async Task<IList<QueryResult>> Execute(string sqlString)
        {
            Console.WriteLine("");
            Console.WriteLine(sqlString);
            var results = new List<QueryResult>();

            try
            {
                AntlrInputStream inputStream = new AntlrInputStream(sqlString);                
                BareBonesSqlLexer lexer = new BareBonesSqlLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                BareBonesSqlParser parser = new BareBonesSqlParser(commonTokenStream);

                var context = parser.sql_stmt_list();
                var builder = (Builder)_visitor.Visit(context);
                _transformer.TransformBuilder(builder);
                builder.BuildSqlStatements(_generator);


                foreach (var sqlCommand in builder.SqlCommands)
                {
                    try
                    {
                        Console.WriteLine("");
                        string sqlTextToExecute = "";
                        if (sqlCommand is DatabaseSqlCommand)
                            _databaseName = ((DatabaseSqlCommand)sqlCommand).DatabaseName;

                        switch (sqlCommand)
                        {
                            case ReadQuerySqlCommand readQuerySql:
                                sqlTextToExecute = readQuerySql.TransformedSqlStatementText[0];
                                Console.WriteLine(sqlTextToExecute);
                                var resultList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                                var queryResult = _infoPostProcessing.TranslateSelectResults(readQuerySql, resultList, _databaseName);
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
                                Console.WriteLine(sqlTextToExecute);

                                var resultsList = await _connector.ExecuteQuery(sqlTextToExecute, _databaseName);
                                var finalListOfUpdates = _infoPostProcessing.UpdateUpdateRecordStatement(updateSqlCommand, resultsList, _databaseName);

                                var updatesToExecute = finalListOfUpdates.Select(u => _generator.BuildString(u)).ToList();

                                foreach (var updateToExecute in updatesToExecute)
                                {
                                    Console.WriteLine(updateToExecute);
                                    await _connector.ExecuteCommand(updateToExecute, _databaseName);

                                }
                                results.Add(CreateQueryResult(true, updateSqlCommand.OriginalSqlStatement.GetStatementType()));
                                break;


                            case GenericSqlCommand genericSqlCommand:
                                for (int i = 0; i < genericSqlCommand.TransformedSqlStatement.Count; i++)
                                {
                                    sqlTextToExecute = genericSqlCommand.TransformedSqlStatementText[i];
                                    Console.WriteLine(sqlTextToExecute);
                                    await _connector.ExecuteCommand(sqlTextToExecute, _databaseName);
                                }
                                results.Add(CreateQueryResult(true, genericSqlCommand.OriginalSqlStatement.GetStatementType()));
                                break;

                            case DatabaseSqlCommand databaseSqlCommand:
                                if (databaseSqlCommand.OriginalSqlStatement is UseDatabaseStatement)
                                {
                                    results.Add(CreateQueryResult(true, databaseSqlCommand.OriginalSqlStatement.GetStatementType()));
                                    continue;
                                }
                                
                                if(databaseSqlCommand.OriginalSqlStatement is CreateDatabaseStatement)
                                    await _connector.InsertToDatabasesTable(((CreateDatabaseStatement)databaseSqlCommand.TransformedSqlStatement[0]).DatabaseName.Value);
                                
                                else if(databaseSqlCommand.OriginalSqlStatement is DropDatabaseStatement)
                                    await _connector.DeleteFromDatabasesTable(((DropDatabaseStatement)databaseSqlCommand.TransformedSqlStatement[0]).DatabaseName.Value);

                                for (int i = 0; i < databaseSqlCommand.TransformedSqlStatement.Count; i++)
                                {
                                    sqlTextToExecute = databaseSqlCommand.TransformedSqlStatementText[i];
                                    Console.WriteLine(sqlTextToExecute);
                                    if (databaseSqlCommand.TransformedSqlStatement[i] is ISqlDatabaseStatement)
                                        await _connector.ExecuteCommand(sqlTextToExecute, null);
                                    else
                                        await _connector.ExecuteCommand(sqlTextToExecute, _databaseName);
                                }
                                results.Add(CreateQueryResult(true, databaseSqlCommand.OriginalSqlStatement.GetStatementType()));
                                break;
                            case ListOrDiscoverCurrentDatabaseCommand listOrDiscoverCurrentDatabase:
                                if (listOrDiscoverCurrentDatabase.OriginalSqlStatement is ListDatabasesStatement)
                                {
                                    var databasesList = _infoPostProcessing.GetDatabasesList();
                                    Console.WriteLine("Databases:");
                                    foreach (var database in databasesList) Console.WriteLine(database);
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
                        results.Add(CreateQueryResult(false, sqlCommand.OriginalSqlStatement.GetStatementType(), e.Message));
                    }
                }
            }
            catch (Exception e)
            {
                results.Add(CreateQueryResult(false, "script", e.Message));
            }
            return results;
        }

        private QueryResult CreateQueryResult(bool success, string statementType, string exceptionMessage = null)
        {
            var executed = success ? "True" : "False";
            var message = $"The {statementType} statement " + (success ? "executed correctly." : "didn't execute. Exception: " + exceptionMessage);
            return new QueryResult(
                new List<IList<string>>()
                {
                    new List<string>() {executed, message}
                },
                new List<string>() { "Executed", "Message" }
            );
        }

        public IList<DatabasePoco> GetStructure()
        {
            return _infoPostProcessing.GetStructure();
        }

        private async Task SendTransactionToProducers(string queryToExecute, string databaseName)
        {
            // new NetworkMessage(NetworkMessageTypeEnum.SendTransaction, payload, TransportTypeEnum.Tcp, senderPrivateKey, senderPublicKey, senderEndPoint, destination)
        }
    }
}
