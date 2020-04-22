using System.Collections.Generic;
using System.Linq;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class SimpleSelectStatement : ISqlStatement
    {
        public SelectCoreStatement SelectCoreStatement { get; set; }
        public IList<OrderingTerm> OrderingTerms { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }

        public SimpleSelectStatement()
        {
            SelectCoreStatement = new SelectCoreStatement();
            OrderingTerms = new List<OrderingTerm>();
        }

        public ISqlStatement Clone()
        {
            return new SimpleSelectStatement()
            {
                SelectCoreStatement = (SelectCoreStatement) SelectCoreStatement.Clone(),
                OrderingTerms = OrderingTerms?.Select(o => o.Clone()).ToList(),
                Limit = Limit,
                Offset = Offset
            };
        }

        public string GetStatementType()
        {
            return "simple select";
        }
    }
}