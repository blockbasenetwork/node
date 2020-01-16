using System;
using System.Collections.Generic;
using System.Text;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class ListOrDiscoverCurrentDatabaseCommand : ISqlCommand
    {
        public ISqlStatement OriginalSqlStatement { get; set; }
        public IList<ISqlStatement> TransformedSqlStatement { get; set; }
        public IList<string> TransformedSqlStatementText { get; set; }

        public ListOrDiscoverCurrentDatabaseCommand(ListDatabasesStatement listDatabasesStatement)
        {
            TransformedSqlStatement = new List<ISqlStatement>();
            OriginalSqlStatement = listDatabasesStatement;
        }

        public ListOrDiscoverCurrentDatabaseCommand(CurrentDatabaseStatement currentDatabaseStatement)
        {
            TransformedSqlStatement = new List<ISqlStatement>();
            OriginalSqlStatement = currentDatabaseStatement;
        }
    }
}
