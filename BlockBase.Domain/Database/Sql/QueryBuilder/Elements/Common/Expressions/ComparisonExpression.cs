using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions
{
    public class ComparisonExpression : AbstractExpression
    {
        public TableAndColumnName LeftTableNameAndColumnName { get; set; }

        public TableAndColumnName RightTableNameAndColumnName { get; set; }

        public bool HasParenthesis { get; set; }

        

        public Value Value { get; set; }

        public ComparisonOperatorEnum ComparisonOperator { get; set; }

        public ComparisonExpression() { }

        public ComparisonExpression(ComparisonOperatorEnum comparisonOperator, bool hasParenthesis)
        {
            ComparisonOperator = comparisonOperator;
            HasParenthesis = hasParenthesis;

        }

        public ComparisonExpression(TableAndColumnName tableAndColumnName, Value value, ComparisonOperatorEnum comparisonOperator, bool hasParenthesis = false) : this(comparisonOperator, hasParenthesis)
        {
            LeftTableNameAndColumnName = tableAndColumnName;
            Value = value;
        }

        public ComparisonExpression(TableAndColumnName leftTableAndColumnName, TableAndColumnName rightTableAndColumnName, ComparisonOperatorEnum comparisonOperator, bool hasParenthesis = false) : this(comparisonOperator, hasParenthesis)
        {
            LeftTableNameAndColumnName = leftTableAndColumnName;
            RightTableNameAndColumnName = rightTableAndColumnName;
        }

        public AbstractExpression Clone()
        {
            return new ComparisonExpression() { LeftTableNameAndColumnName = LeftTableNameAndColumnName.Clone(), RightTableNameAndColumnName = RightTableNameAndColumnName != null ? RightTableNameAndColumnName.Clone() : null, Value = Value != null ? Value.Clone() : null, ComparisonOperator = ComparisonOperator, HasParenthesis = HasParenthesis };
        }

        public enum ComparisonOperatorEnum
        {
            Is,
            IsNot,
            SmallerThan,
            SmallerOrEqualThan,
            BiggerThan,
            BiggerOrEqualThan,
            Equal,
            Different
        }
    }
}