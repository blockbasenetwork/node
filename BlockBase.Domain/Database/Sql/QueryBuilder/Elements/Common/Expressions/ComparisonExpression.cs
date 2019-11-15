namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions
{
    public class ComparisonExpression : AbstractExpression
    {
        public estring TableName { get; set; }

        public estring ColumnName { get; set; }

        public string Value { get; set; }

        public ComparisonOperatorEnum LogicalOperator { get; set; }

        public AbstractExpression Clone()
        {
            return new ComparisonExpression() { TableName = TableName.Clone(), ColumnName = ColumnName.Clone(), Value = Value, LogicalOperator = LogicalOperator };
        }

        public enum ComparisonOperatorEnum
        {
            SmallerThan,
            SmallerOrEqualThan,
            BiggerThan,
            BiggerOrEqualThan,
            Equal,
            Different
        }
    }
}