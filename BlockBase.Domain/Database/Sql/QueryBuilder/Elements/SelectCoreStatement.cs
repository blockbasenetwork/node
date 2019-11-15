using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class SelectCoreStatement : ISqlStatement
    {
        public IList<ResultColumn> ResultColumns { get; set; }
        public IList<TableOrSubquery> TablesOrSubqueries { get; set; }
        public JoinClause JoinClause { get; set; }
        public AbstractExpression WhereExpression { get; set; }
        public bool DistinctFlag { get; set; }

        public SelectCoreStatement()
        {
            DistinctFlag = false;
            ResultColumns = new List<ResultColumn>();
            TablesOrSubqueries = new List<TableOrSubquery>();
        }

        public ISqlStatement Clone()
        {
            return new SelectCoreStatement()
            {

                ResultColumns = ResultColumns.Select(r => r.Clone()).ToList(),
                TablesOrSubqueries = TablesOrSubqueries.Select(t => t.Clone()).ToList(),
                JoinClause = JoinClause?.Clone(),
                WhereExpression = WhereExpression?.Clone(),
                DistinctFlag = DistinctFlag
            };
        }
    }
}