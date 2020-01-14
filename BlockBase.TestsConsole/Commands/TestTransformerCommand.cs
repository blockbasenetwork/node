using Antlr4.Runtime;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Database.QueryParser;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryParser;
using BlockBase.TestsConsole.Commands.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using BlockBase.Domain.Database.Sql.SqlCommand;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using System.Collections.Generic;

namespace BlockBase.TestsConsole.Commands
{
    internal class TestTransformerCommand : IHelperCommand
    {
        private Transformer_v2 _transformer;
        private BareBonesSqlBaseVisitor<object> _visitor;
        private PSqlConnector _psqlConnector;
        private InfoPostProcessing _infoPostProcessing;
        private readonly ILogger _logger;
        private string _databaseName = "";

        public TestTransformerCommand(ILogger logger)
        {
            _logger = logger;
            _psqlConnector = new PSqlConnector("localhost", "postgres", 5432, "qwerty123", _logger);
            var secretStore = new SecretStore();
            secretStore.SetSecret("master_key", KeyAndIVGenerator_v2.CreateRandomKey());
            secretStore.SetSecret("master_iv", KeyAndIVGenerator_v2.CreateMasterIV("qwerty123"));
            var databaseKeyManager = new DatabaseKeyManager(secretStore);
            var middleMan = new MiddleMan(databaseKeyManager, secretStore);
            _transformer = new Transformer_v2(_psqlConnector, middleMan);
            _visitor = new BareBonesSqlVisitor();
            _infoPostProcessing = new InfoPostProcessing(middleMan);
        }

        public async Task ExecuteAsync()
        {
            RunSqlCommand("CREATE DATABASE database1;");
            RunSqlCommand("USE database1;");

            RunSqlCommand("CREATE TABLE table1 ( position ENCRYPTED RANGE (2, 1, 10) PRIMARY KEY, !column2 ENCRYPTED 4 NOT NULL);");
            RunSqlCommand("CREATE TABLE table2 ( column1 ENCRYPTED RANGE (2, 1, 10) PRIMARY KEY REFERENCES table1 ( position ), column2 ENCRYPTED 40 );");
            //RunSqlCommand("CREATE TABLE table3 ( column1 ENCRYPTED 5 PRIMARY KEY REFERENCES table1 ( position ), column2 ENCRYPTED 40 );");
            RunSqlCommand("CREATE TABLE !table4 ( !column1 int PRIMARY KEY);");
            RunSqlCommand("CREATE TABLE !table5 ( !column1 int PRIMARY KEY REFERENCES !table4 ( !column1 ), column2 ENCRYPTED 40 );");
            RunSqlCommand("CREATE TABLE !table6 ( !column1 int PRIMARY KEY REFERENCES !table4 ( !column1 ), column2 ENCRYPTED 40 );");

            RunSqlCommand("DROP TABLE table2;");

            RunSqlCommand("ALTER TABLE table1 RENAME TO newtable11");
            RunSqlCommand("ALTER TABLE newtable11 RENAME TO bestplayers");
            RunSqlCommand("ALTER TABLE bestplayers RENAME !column2 TO name");
            RunSqlCommand("ALTER TABLE bestplayers ADD COLUMN !number int");
            RunSqlCommand("ALTER TABLE bestplayers ADD COLUMN column4 ENCRYPTED 30 NOT NULL");
            RunSqlCommand("ALTER TABLE bestplayers DROP COLUMN column4");

            RunSqlCommand("INSERT INTO !table4 (!column1) VALUES ( 1 )");
            RunSqlCommand("INSERT INTO !table4 (!column1) VALUES ( 2 )");
            RunSqlCommand("INSERT INTO !table4 (!column1) VALUES ( 3 )");

            RunSqlCommand("INSERT INTO !table5 (!column1, column2) VALUES ( 1, 'primeiro' )");
            RunSqlCommand("INSERT INTO !table5 (!column1, column2) VALUES ( 2, 'segundo' )");
            RunSqlCommand("INSERT INTO !table5 (!column1, column2) VALUES ( 3, 'terceiro' )");

            RunSqlCommand("INSERT INTO !table6 (!column1, column2) VALUES ( 1, 'first' )");
            RunSqlCommand("INSERT INTO !table6 (!column1, column2) VALUES ( 2, 'second' )");
            RunSqlCommand("INSERT INTO !table6 (!column1, column2) VALUES ( 3, 'third' )");

            RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 1, 'Cristiano Ronaldo', 7 )");
            RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 2, 'bulha', 7 )");
            RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 3, 'bulha', 25 )");
            RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 4, 'pires', 10 )");
            RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 5, 'fernando', 25 )");
            RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 6, 'marcia', 26 )");
            RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 7, 'marcia', 290)");

            RunSqlCommand("SELECT bestplayers.name FROM bestplayers WHERE bestplayers.name == 'bulha';");

            RunSqlCommand("SELECT bestplayers.* FROM bestplayers WHERE bestplayers.name == 'bulha' OR ( bestplayers.!number > 10 AND bestplayers.!number <= 26 );");

            RunSqlCommand("SELECT bestplayers.* FROM bestplayers");

            RunSqlCommand("SELECT !table4.*, !table5.* FROM !table4 JOIN !table5 ON !table4.!column1 == !table5.!column1;");

            RunSqlCommand("SELECT !table4.*, !table5.* FROM !table4 JOIN !table5 ON !table4.!column1 == !table5.!column1 AND !table4.!column1 == 2 ;");

            RunSqlCommand("SELECT !table4.*, !table5.* FROM !table4 JOIN !table5 ON !table4.!column1 == !table5.!column1 WHERE !table4.!column1 == 2;");

            RunSqlCommand("SELECT !table4.*, !table5.column2, !table6.column2  FROM !table4 JOIN !table5 ON !table4.!column1 == !table5.!column1 JOIN !table6 ON !table4.!column1 == !table6.!column1 WHERE !table4.!column1 == 2;");

            RunSqlCommand("SELECT !table5.column2, !table6.column2  FROM !table5 JOIN !table6 ON !table5.column2 == !table6.column2;");

            RunSqlCommand("SELECT !table5.column2, !table6.column2  FROM !table5, !table6 WHERE !table5.!column1 == !table6.!column1;");

            //RunSqlCommand("UPDATE bestplayers SET name = 'ricardo' where bestplayers.name == 'marcia'");

            //RunSqlCommand("DROP DATABASE database1;");
        }

        private void RunSqlCommand(string plainSqlCommand)
        {
            Console.WriteLine("");
            Console.WriteLine(plainSqlCommand);
            AntlrInputStream inputStream = new AntlrInputStream(plainSqlCommand);
            BareBonesSqlLexer lexer = new BareBonesSqlLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            BareBonesSqlParser parser = new BareBonesSqlParser(commonTokenStream);

            var context = parser.sql_stmt_list();
            try
            {
                var builder = (Builder)_visitor.Visit(context);
                _transformer.TransformBuilder(builder);
                builder.BuildSqlStatements(new PSqlGenerator());


                foreach (var sqlCommand in builder.SqlCommands)
                {
                    Console.WriteLine("");
                    string sqlTextToExecute = "";
                    if (sqlCommand is DatabaseSqlCommand)              
                        _databaseName =( (DatabaseSqlCommand)sqlCommand).DatabaseName;
                        
                    switch (sqlCommand)
                    {
                        case ReadQuerySqlCommand readQuerySql:
                            sqlTextToExecute = readQuerySql.TransformedSqlStatementText[0];
                            Console.WriteLine(sqlTextToExecute);
                            var resultList = _psqlConnector.ExecuteQuery(sqlTextToExecute, _databaseName);
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
                            var resultsList = _psqlConnector.ExecuteQuery(sqlTextToExecute, _databaseName);
                            var descryptedResults = _infoPostProcessing.DecryptRows((SimpleSelectStatement)updateSqlCommand.TransformedSqlStatement[0], resultsList, _databaseName, out IList<string> columnNames);
                            foreach (var columnName in columnNames) Console.WriteLine(columnName + " ");
                            foreach (var row in descryptedResults)
                            {
                                Console.WriteLine();
                                foreach (var value in row) Console.Write(value + " ");
                            }
                            break;


                        case GenericSqlCommand genericSqlCommand:
                            for (int i = 0; i < genericSqlCommand.TransformedSqlStatement.Count; i++)
                            {
                                sqlTextToExecute = genericSqlCommand.TransformedSqlStatementText[i];
                                Console.WriteLine(sqlTextToExecute);
                                _psqlConnector.ExecuteCommand(sqlTextToExecute, _databaseName);
                            }
                            break;

                        case DatabaseSqlCommand databaseSqlCommand:
                            if (databaseSqlCommand.OriginalSqlStatement is UseDatabaseStatement) continue;
                            for (int i = 0; i < databaseSqlCommand.TransformedSqlStatement.Count; i++)
                            {
                                sqlTextToExecute = databaseSqlCommand.TransformedSqlStatementText[i];
                                Console.WriteLine(sqlTextToExecute);
                                if (databaseSqlCommand.TransformedSqlStatement[i] is ISqlDatabaseStatement)
                                    _psqlConnector.ExecuteCommand(sqlTextToExecute, null);
                                 else
                                    _psqlConnector.ExecuteCommand(sqlTextToExecute, _databaseName);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public string GetCommandHelp()
        {
            return "tt";
        }

        public bool TryParseCommand(string commandStr)
        {
            return "tt" == commandStr;
        }
    }
}