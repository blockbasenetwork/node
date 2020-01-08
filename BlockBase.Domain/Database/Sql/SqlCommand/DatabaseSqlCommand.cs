using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class DatabaseSqlCommand : ISqlCommand
    {
        public IList<string> TransformedSqlStatementText { get; set; }
        public string DatabaseName { get; set; }

        public ISqlStatement OriginalSqlStatement { get; set; }
        public IList<ISqlStatement> TransformedSqlStatement { get; set; }

        public DatabaseSqlCommand(ISqlDatabaseStatement databaseStatement)
        {
            OriginalSqlStatement = databaseStatement;
        }
    }
}
