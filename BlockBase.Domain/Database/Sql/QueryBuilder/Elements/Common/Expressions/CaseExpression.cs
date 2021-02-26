using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions
{
    public class CaseExpression : AbstractExpression
    {
        public bool HasParenthesis { get; set; }
        public IEnumerable<WhenThenExpression> WhenThenExpressions { get; set;}

        public LiteralValueExpression ElseExpression { get; set;}
        public CaseExpression() { }

        public CaseExpression(IEnumerable<WhenThenExpression> whenThenExpressions, LiteralValueExpression elseExpression, bool? hasParenthesis = null)
        {
            WhenThenExpressions = whenThenExpressions;
            ElseExpression = elseExpression;
            HasParenthesis = hasParenthesis ?? false;
        }
        public AbstractExpression Clone()
        {
            return new CaseExpression() {  HasParenthesis = HasParenthesis };
        }
    }
}