using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class TableOrSubquery
    {
        public estring TableName { get; set; }
        public IList<TableOrSubquery> TablesOrSubqueries { get; set; }
        public JoinClause JoinClause { get; set; }
        public SimpleSelectStatement SimpleSelectStatement { get; set; }

        public TableOrSubquery Clone()
        {
            return new TableOrSubquery()
            {
                TableName = TableName?.Clone(),
                TablesOrSubqueries = TablesOrSubqueries?.Select(t => (TableOrSubquery) t.Clone()).ToList(),
                JoinClause = JoinClause?.Clone(),
                SimpleSelectStatement = (SimpleSelectStatement) SimpleSelectStatement?.Clone()
            };
        }
    }
}