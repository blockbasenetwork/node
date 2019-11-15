using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public abstract class AbstractAlterTableStatement : ISqlStatement
    {
        public estring TableName { get; set; }

        public abstract ISqlStatement Clone();
        
    }
}