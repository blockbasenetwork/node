using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class ChangeRecordSqlCommand : ISqlCommand
    {
        public IList<string> TransformedSqlStatementText { get; set; }

        public ISqlStatement OriginalSqlStatement { get; set; }
        public IList<ISqlStatement> TransformedSqlStatement { get; set; }

        public ChangeRecordSqlCommand(IChangeRecordStatement changeRecordStatement)
        {
            OriginalSqlStatement = changeRecordStatement;
        }

    }
}
