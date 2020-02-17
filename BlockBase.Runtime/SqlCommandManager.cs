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
using System.Collections.Concurrent;
using System.Threading;
using BlockBase.Utils.Threading;


namespace BlockBase.Runtime
{
    public class SqlCommandManager
    {
        private Transformer _transformer;
        private IGenerator _generator;
        private InfoPostProcessing _infoPostProcessing;
        private BareBonesSqlBaseVisitor<object> _visitor;
        private IConnector _connector;
        private ILogger _logger;
        private int _transaction_sequence_number = 0;
        private DatabaseAccess _databaseAccess;


        // public SqlCommandManager(MiddleMan middleMan, ILogger logger, IConnector connector, INetworkService networkService)
        public SqlCommandManager(MiddleMan middleMan, ILogger logger, IConnector connector, DatabaseAccess databaseAccess)
        {
            _visitor = new BareBonesSqlVisitor();
            _infoPostProcessing = new InfoPostProcessing(middleMan);
            _generator = new PSqlGenerator();
            _logger = logger;
            _connector = connector;
            _transformer = new Transformer(middleMan);
            _databaseAccess = databaseAccess;
        }

        public async Task<IList<QueryResult>> Execute(string sqlString)
        {
            Console.WriteLine("");
            Console.WriteLine(sqlString);
            IList<QueryResult> results = new List<QueryResult>();

            try
            {
                AntlrInputStream inputStream = new AntlrInputStream(sqlString);
                BareBonesSqlLexer lexer = new BareBonesSqlLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                BareBonesSqlParser parser = new BareBonesSqlParser(commonTokenStream);

                var context = parser.sql_stmt_list();
                var builder = (Builder)_visitor.Visit(context);
                var executioner = new StatementExecutionManager(_transformer, _generator, _logger, _connector, _infoPostProcessing, _databaseAccess);
                results = await executioner.ExecuteBuilder(builder, CreateQueryResult);
                
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
