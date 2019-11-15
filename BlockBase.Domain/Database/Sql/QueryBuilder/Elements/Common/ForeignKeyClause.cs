using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class ForeignKeyClause
    {
        public estring ForeignTableName { get; set; }

        public IList<estring> ColumnNames { get; set; }

        public ForeignKeyClause Clone()
        {
            return new ForeignKeyClause() { ForeignTableName = ForeignTableName.Clone(), ColumnNames = ColumnNames.Select(c => c.Clone()).ToList() };
        }
    }
}