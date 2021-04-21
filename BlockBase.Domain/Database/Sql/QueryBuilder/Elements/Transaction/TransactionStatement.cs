using System.Collections.Generic;
using System.Linq;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Transaction
{
    public class TransactionStatement : ISqlStatement
    {
        public List<ISqlStatement> OperationStatements { get; set; }
        public TransactionStatement()
        {
            OperationStatements = new List<ISqlStatement>();
        }

        public ISqlStatement Clone()
        {
            return new TransactionStatement()
            {
                OperationStatements = OperationStatements
            };
        }

        public string GetStatementType()
        {
            return "transaction";
        }
    }
}