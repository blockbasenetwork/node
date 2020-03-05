
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record
{
    public interface IChangeRecordStatement : ISqlStatement
    {
        AbstractExpression WhereExpression { get; set; }
        estring TableName { get; set; }
    }
}