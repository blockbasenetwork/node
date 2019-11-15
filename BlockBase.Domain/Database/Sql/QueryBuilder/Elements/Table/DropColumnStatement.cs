using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class DropColumnStatement : AbstractAlterTableStatement
    {
        public estring ColumnName { get; set; }

        public override ISqlStatement Clone()
        {
            return new DropColumnStatement() { ColumnName = ColumnName.Clone(), TableName = TableName.Clone() };
        }
    }
}