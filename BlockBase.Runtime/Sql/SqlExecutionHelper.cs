using System;
using System.Threading.Tasks;
using Antlr4.Runtime;
using BlockBase.DataPersistence.Data.MongoDbEntities;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain.Database.QueryParser;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryParser;

namespace BlockBase.Runtime.Sql
{
    public class SqlExecutionHelper
    {
        private IConnector _connector;
        private BareBonesSqlBaseVisitor<object> _visitor;
        
        public SqlExecutionHelper( IConnector connector)
        {
            _connector = connector;
            _visitor = new BareBonesSqlVisitor();
        }


        public Domain.Database.Sql.QueryBuilder.Builder ParseSqlText(string sqlString)
        {
            AntlrInputStream inputStream = new AntlrInputStream(sqlString);
            BareBonesSqlLexer lexer = new BareBonesSqlLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            BareBonesSqlParser parser = new BareBonesSqlParser(commonTokenStream);
            var context = parser.sql_stmt_list();
            return (Domain.Database.Sql.QueryBuilder.Builder)_visitor.Visit(context);
        }


        public async Task<bool> HasTransactionBeenExecuted(TransactionDB pendingTransaction)
        {
            var builder = ParseSqlText(pendingTransaction.TransactionJson);
            var sqlStatement = builder.SqlCommands[0].OriginalSqlStatement;

            var createDatabaseStatement = sqlStatement as CreateDatabaseStatement;
            var dropDatabaseStatement = sqlStatement as DropDatabaseStatement;

            if (createDatabaseStatement != null)
                return await _connector.DoesDatabaseExist(createDatabaseStatement.DatabaseName.Value);

            if (dropDatabaseStatement != null)
                return !await _connector.DoesDatabaseExist(dropDatabaseStatement.DatabaseName.Value);


            return await _connector.WasTransactionExecuted(pendingTransaction.DatabaseName, Convert.ToUInt64(pendingTransaction.SequenceNumber));
        }

    }
}