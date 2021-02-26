using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions
{
    public class WhenThenExpression : AbstractExpression
    {
        public bool HasParenthesis { get; set; }
        public ComparisonExpression WhenExpression { get; set; }
        public LiteralValueExpression ThenExpression { get; set; }

        public WhenThenExpression() { }

        public WhenThenExpression(ComparisonExpression whenExpression, LiteralValueExpression thenExpression,  bool? hasParenthesis = null)
        {
            WhenExpression = whenExpression;
            ThenExpression = thenExpression;
            HasParenthesis = hasParenthesis ?? false;
        }
        public AbstractExpression Clone()
        {
            return new WhenThenExpression() { HasParenthesis = HasParenthesis };
        }
    }
}