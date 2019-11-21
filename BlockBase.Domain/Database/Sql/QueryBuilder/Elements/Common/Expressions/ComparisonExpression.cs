namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions
{
    public class ComparisonExpression : AbstractExpression
    {
        public estring TableName { get; set; }

        public estring ColumnName { get; set; }

        public string Value { get; set; }

        public ComparisonOperatorEnum ComparisonOperator { get; set; }

        public ComparisonExpression() { }

        public ComparisonExpression(estring tableName, estring columnName, string value, ComparisonOperatorEnum comparisonOperator)
        {
            TableName = tableName;
            ColumnName = columnName;
            Value = value;
            ComparisonOperator = comparisonOperator;
        }

        public AbstractExpression Clone()
        {
            return new ComparisonExpression() { TableName = TableName.Clone(), ColumnName = ColumnName.Clone(), Value = Value, ComparisonOperator = ComparisonOperator };
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