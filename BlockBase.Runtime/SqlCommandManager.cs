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
using BlockBase.Runtime.Network;

namespace BlockBase.Runtime
{
    public class SqlCommandManager
    {
        private Transformer_v2 _transformer;
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
            _transformer = new Transformer_v2(middleMan);
        }

        public async Task Execute(string sqlString)
        {
            Console.WriteLine("");
            Console.WriteLine(sqlString);
            AntlrInputStream inputStream = new AntlrInputStream(sqlString);
            BareBonesSqlLexer lexer = new BareBonesSqlLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            BareBonesSqlParser parser = new BareBonesSqlParser(commonTokenStream);

            var context = parser.sql_stmt_list();
            try
            {
                var builder = (Builder)_visitor.Visit(context);
                _transformer.TransformBuilder(builder);
                builder.BuildSqlStatements(_generator);


                foreach (var sqlCommand in builder.SqlCommands)
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
                            var filteredResults = _infoPostProcessing.TranslateSelectResults(readQuerySql, resultList, _databaseName);
                            foreach (var row in filteredResults)
                            {
                                Console.WriteLine();
                                foreach (var value in row) Console.Write(value + " ");
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

                            break;


                        case GenericSqlCommand genericSqlCommand:
                            for (int i = 0; i < genericSqlCommand.TransformedSqlStatement.Count; i++)
                            {
                                sqlTextToExecute = genericSqlCommand.TransformedSqlStatementText[i];
                                Console.WriteLine(sqlTextToExecute);
                                await _connector.ExecuteCommand(sqlTextToExecute, _databaseName);
                            }
                            break;

                        case DatabaseSqlCommand databaseSqlCommand:
                            if (databaseSqlCommand.OriginalSqlStatement is UseDatabaseStatement) continue;
                            for (int i = 0; i < databaseSqlCommand.TransformedSqlStatement.Count; i++)
                            {
                                sqlTextToExecute = databaseSqlCommand.TransformedSqlStatementText[i];
                                Console.WriteLine(sqlTextToExecute);
                                if (databaseSqlCommand.TransformedSqlStatement[i] is ISqlDatabaseStatement)
                                    await _connector.ExecuteCommand(sqlTextToExecute, null);
                                else
                                    await _connector.ExecuteCommand(sqlTextToExecute, _databaseName);
                            }
                            break;
                        case ListOrDiscoverCurrentDatabaseCommand listOrDiscoverCurrentDatabase:
                            if (listOrDiscoverCurrentDatabase.OriginalSqlStatement is ListDatabasesStatement)
                            {
                               
                                var databasesList = _infoPostProcessing.GetDatabasesList();
                                Console.WriteLine("Databases:");
                                foreach (var database in databasesList) Console.WriteLine(database);
                            }

                            else Console.WriteLine("Current Database: " + _infoPostProcessing.DecryptDatabaseName(_databaseName) ?? "none");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
