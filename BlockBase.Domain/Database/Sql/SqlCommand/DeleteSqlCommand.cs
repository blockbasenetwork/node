using System.Collections.Generic;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class DeleteSqlCommand : ISqlCommand
    {
        public IList<string> TransformedSqlStatementText { get; set; }

        public ISqlStatement OriginalSqlStatement { get; set; }
        public IList<ISqlStatement> TransformedSqlStatement { get; set; }

        public DeleteSqlCommand(DeleteRecordStatement deleteRecordStatement)
        {
            OriginalSqlStatement = deleteRecordStatement;
        }
    }
}
