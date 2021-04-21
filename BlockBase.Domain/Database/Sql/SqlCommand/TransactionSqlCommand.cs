using System;
using System.Collections.Generic;
using System.Text;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Transaction;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class TransactionSqlCommand : ISqlCommand
    {
        public IList<string> TransformedSqlStatementText { get; set; }

        public ISqlStatement OriginalSqlStatement { get; set; }
        public IList<ISqlStatement> TransformedSqlStatement { get; set; }

        public TransactionSqlCommand(TransactionStatement transactionStatement)
        {
            OriginalSqlStatement = transactionStatement;
        }
    }
}
