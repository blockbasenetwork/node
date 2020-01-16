using System;
using System.Collections.Generic;
using System.Text;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class ReadQuerySqlCommand : ISqlCommand
    {
        public IList<string> TransformedSqlStatementText { get; set; }

        public ISqlStatement OriginalSqlStatement { get; set; }
        public IList<ISqlStatement> TransformedSqlStatement { get; set; }

        public ReadQuerySqlCommand(SimpleSelectStatement simpleSelectStatement)
        {
            OriginalSqlStatement = simpleSelectStatement;
        }
    }
}
