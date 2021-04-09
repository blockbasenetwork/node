using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Transaction
{
    public class BeginStatement : ISqlStatement
    {

        public BeginStatement() { }

        public ISqlStatement Clone()
        {
            return new BeginStatement() {  };
        }

        public string GetStatementType()
        {
            return "begin";
        }
    }
}