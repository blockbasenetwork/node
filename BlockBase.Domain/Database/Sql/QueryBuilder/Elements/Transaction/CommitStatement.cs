using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Transaction
{
    public class CommitStatement : ISqlStatement
    {

        public CommitStatement() { }

        public ISqlStatement Clone()
        {
            return new CommitStatement() {  };
        }

        public string GetStatementType()
        {
            return "commit";
        }
    }
}