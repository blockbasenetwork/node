using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record
{
    public class DeleteRecordStatement : ISqlStatement
    {
        public estring TableName { get; set; }
        public AbstractExpression WhereClause { get; set; }

        public ISqlStatement Clone()
        {
            return new DeleteRecordStatement() { TableName = TableName.Clone(), WhereClause = WhereClause.Clone() };
        }
    }
}