using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions
{
    public class LiteralValueExpression : AbstractExpression
    {
        public bool HasParenthesis { get; set; }
        public Value LiteralValue { get; set; }

        public LiteralValueExpression() { }

        public LiteralValueExpression(Value literalValue, bool? hasParenthesis = null)
        {
            LiteralValue = literalValue;
            HasParenthesis = hasParenthesis ?? false;
        }
        public AbstractExpression Clone()
        {
            return new LiteralValueExpression() { LiteralValue = LiteralValue.Clone(), HasParenthesis = HasParenthesis };
        }
    }
}
