using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class DropTableStatement : ISqlStatement
    {
        public estring TableName { get; set; }

        public DropTableStatement Clone()
        {
            return new DropTableStatement() { TableName = TableName.Clone() };
        }
    }
}