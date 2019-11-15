using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class RenameTableStatement : AbstractAlterTableStatement
    {
        public estring NewTableName { get; set; }

        public override ISqlStatement Clone()
        {
            return new RenameTableStatement() { TableName = TableName.Clone(), NewTableName = NewTableName.Clone() };
        }
    }
}