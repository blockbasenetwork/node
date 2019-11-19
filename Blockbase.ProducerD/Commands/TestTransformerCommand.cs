﻿using Antlr4.Runtime;
using BlockBase.Domain.Database.QueryParser;
using BlockBase.Domain.Database.Sql.QueryParser;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy.Encryption;
using Blockbase.ProducerD.Commands.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;

namespace Blockbase.ProducerD.Commands
{
    class TestTransformerCommand : IHelperCommand
    {
        private Transformer _transformer;
        private BareBonesSqlBaseVisitor<object> _visitor;
        private PSqlConnector _psqlConnector;
        private ILogger _logger;

        public TestTransformerCommand(ILogger logger)
        {
            _logger = logger;
            _psqlConnector = new PSqlConnector("localhost", "postgres", 5432, "qwerty123", _logger);
            _transformer = new Transformer(_psqlConnector);
            _visitor = new BareBonesSqlVisitor();
        }


        public async Task ExecuteAsync()
        {
            RunSqlCommand("CREATE DATABASE database1;");

            RunSqlCommand("CREATE TABLE table1 ( column1 ENCRYPTED 2 RANGE (1,10) PRIMARY KEY, column2 ENCRYPTED 30 NOT NULL);");
            RunSqlCommand("CREATE TABLE table2 ( column1 ENCRYPTED 2 RANGE (2,10) PRIMARY KEY REFERENCES table1 ( column1 ), column2 ENCRYPTED 5 );");

            RunSqlCommand("ALTER TABLE table1 RENAME TO newtable11");
            RunSqlCommand("ALTER TABLE newtable11 RENAME TO newtable1");
            RunSqlCommand("ALTER TABLE newtable1 RENAME column2 TO newcolumn2");
            RunSqlCommand("ALTER TABLE newtable1 ADD COLUMN !column3 int");
            RunSqlCommand("ALTER TABLE newtable1 ADD COLUMN column4 ENCRYPTED 2 NOT NULL");
            RunSqlCommand("ALTER TABLE newtable1 DROP COLUMN column4");

            RunSqlCommand("INSERT INTO newtable1(column1, newcolumn2, !column3) VALUES ( 1, 'bulha', 7 )");

            RunSqlCommand("INSERT INTO newtable1 (column1, newcolumn2, !column3) VALUES ( 2, 'bulha', 5 )");
            RunSqlCommand("INSERT INTO newtable1 (column1, newcolumn2, !column3) VALUES ( 3, 'pires', 10 )");
            RunSqlCommand("INSERT INTO newtable1 (column1, newcolumn2, !column3) VALUES ( 4, 'fernando', 25 )");
            RunSqlCommand("INSERT INTO newtable1 (column1, newcolumn2, !column3) VALUES ( 5, 'marcia', 26 )");
            RunSqlCommand("INSERT INTO newtable1 (column1, newcolumn2, !column3) VALUES ( 6, 'marcia', 2 6)");



            RunSqlCommand("DROP DATABASE database1;");
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
                _transformer.Transform(builder);
                var sqlCommandsPerDatabase = builder.BuildQueryStrings(new PSqlGenerator());
                foreach (var databaseSqlCommandsKeyPair in sqlCommandsPerDatabase)
                {
                    foreach (var transformedSqlCommand in databaseSqlCommandsKeyPair.Value)
                    {
                        Console.WriteLine(transformedSqlCommand.Value);
                        _psqlConnector.ExecuteCommand(transformedSqlCommand.Value, transformedSqlCommand.IsDatabaseStatement ? null : databaseSqlCommandsKeyPair.Key);
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
