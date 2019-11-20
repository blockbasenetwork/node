using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class DropColumnStatement : AbstractAlterTableStatement
    {
        public estring ColumnName { get; set; }

        public DropColumnStatement() { }

        public DropColumnStatement(estring tableName, estring columnName)
        {
            ColumnName = columnName;
            TableName = tableName;
        }

        public override ISqlStatement Clone()
        {
            return new DropColumnStatement() { ColumnName = ColumnName.Clone(), TableName = TableName.Clone() };
        }
    }
}