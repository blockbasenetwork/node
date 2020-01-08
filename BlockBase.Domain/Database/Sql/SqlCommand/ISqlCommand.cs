using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public interface ISqlCommand
    {
        ISqlStatement OriginalSqlStatement { get; set; }
        IList<ISqlStatement> TransformedSqlStatement { get; set; }
        IList<string> TransformedSqlStatementText { get; set; }
    }
}
