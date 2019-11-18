using Antlr4.Runtime;
using BlockBase.Domain.Database.QueryParser;
using BlockBase.Domain.Database.Sql.QueryParser;
using Blockbase.ProducerD.Commands.Interfaces;
using System.Threading.Tasks;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using System;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.DataPersistence.Sidechain.Connectors;
using Microsoft.Extensions.Logging;
using BlockBase.DataProxy.Encryption;

namespace Blockbase.ProducerD.Commands
{
    public class TestBareBonesSqlCommand : IHelperCommand
    {
        public string SqlCommand { get; set; }
        private Transformer _transformer;
        private BareBonesSqlBaseVisitor<object> _visitor;
        private PSqlConnector _psqlConnector;
        private ILogger _logger;

        public TestBareBonesSqlCommand(ILogger logger)
        {
            _logger = logger;
            _psqlConnector = new PSqlConnector("localhost", "postgres", 5432, "qwerty123", _logger);
            _transformer = new Transformer(_psqlConnector);
            _visitor = new BareBonesSqlVisitor();
        }

        public async Task ExecuteAsync()
        {
            AntlrInputStream inputStream = new AntlrInputStream(SqlCommand);
            BareBonesSqlLexer lexer = new BareBonesSqlLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            BareBonesSqlParser parser = new BareBonesSqlParser(commonTokenStream);

            var context = parser.sql_stmt_list();
            try
            {
                var builder = (Builder)_visitor.Visit(context);
                _transformer.Transform(builder);
                var sqlQueryStrings = builder.BuildQueryStrings(new PSqlGenerator());
                foreach (var sqlCommands in sqlQueryStrings.Values)
                {
                    foreach (var sqlCommand in sqlCommands)
                    {
                        Console.WriteLine(sqlCommand.Value);
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
            return "tsql <sql command>";
        }

        public bool TryParseCommand(string commandStr)
        {
            if (!commandStr.StartsWith("tsql")) return false;

            SqlCommand = commandStr.Remove(0, 5);
            return true;
        }
    }
}