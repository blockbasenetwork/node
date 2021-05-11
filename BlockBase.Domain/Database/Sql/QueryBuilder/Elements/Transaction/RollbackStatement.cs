using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Transaction
{
    public class RollbackStatement : ISqlStatement
    {

        public RollbackStatement() { }

        public ISqlStatement Clone()
        {
            return new RollbackStatement() {  };
        }

        public string GetStatementType()
        {
            return "rollback";
        }
    }
}