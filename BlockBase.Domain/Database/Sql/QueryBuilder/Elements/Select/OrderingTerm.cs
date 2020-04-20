using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class OrderingTerm
    {
        public AbstractExpression Expression { get; set; }
        public bool IsAscending { get; set; }

        public OrderingTerm Clone()
        {
            return new OrderingTerm() { Expression = Expression.Clone(), IsAscending = IsAscending };
        }
    }
}