using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class ForeignKeyClause
    {
        public estring TableName { get; set; }

        public IList<estring> ColumnNames { get; set; }

        public ForeignKeyClause() { }

        public ForeignKeyClause(estring tableName)
        {
            TableName = tableName;
            ColumnNames = new List<estring>();
        }

        public ForeignKeyClause Clone()
        {
            return new ForeignKeyClause() { TableName = TableName.Clone(), ColumnNames = ColumnNames.Select(c => c.Clone()).ToList() };
        }
    }
}