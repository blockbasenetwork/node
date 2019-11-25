namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions
{
    public class LogicalExpression : AbstractExpression
    {
        public AbstractExpression LeftExpression { get; set; }

        public bool HasParenthesis { get; set; }

        public AbstractExpression RightExpression { get; set; }

        public LogicalOperatorEnum LogicalOperator { get; set; }

        public AbstractExpression Clone()
        {
            return new LogicalExpression() { LeftExpression = LeftExpression.Clone(), RightExpression = RightExpression.Clone(), LogicalOperator = LogicalOperator, HasParenthesis = HasParenthesis };
        }

        public enum LogicalOperatorEnum
        {
            AND,
            OR
        }
    }
}