using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record
{
    public class DeleteRecordStatement : IChangeRecordStatement
    {
        public estring TableName { get; set; }
        public AbstractExpression WhereExpression { get; set; }

        public DeleteRecordStatement() { }

        public DeleteRecordStatement(estring tableName, AbstractExpression whereExpression)
        {
            TableName = tableName;
            WhereExpression = whereExpression;
        }

        public ISqlStatement Clone()
        {
            return new DeleteRecordStatement() { TableName = TableName.Clone(), WhereExpression = WhereExpression.Clone() };
        }

        public string GetStatementType()
        {
            return "delete record";
        }
    }
}