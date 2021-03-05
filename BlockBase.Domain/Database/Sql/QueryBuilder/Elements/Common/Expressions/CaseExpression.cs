using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
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

        public ResultColumn ResultColumn {get; set;}

        public CaseExpression() { }

        public CaseExpression(IEnumerable<WhenThenExpression> whenThenExpressions, LiteralValueExpression elseExpression, ResultColumn resultColumn = null, bool? hasParenthesis = null)
        {
            WhenThenExpressions = whenThenExpressions;
            ElseExpression = elseExpression;
            ResultColumn = resultColumn;
            HasParenthesis = hasParenthesis ?? false;
        }
        public AbstractExpression Clone()
        {
            return new CaseExpression() {  HasParenthesis = HasParenthesis };
        }
    }
}