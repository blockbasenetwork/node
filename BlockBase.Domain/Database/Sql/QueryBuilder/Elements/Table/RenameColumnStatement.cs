using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class RenameColumnStatement : AbstractAlterTableStatement
    {
        public estring ColumnName { get; set; }
        public estring NewColumnName { get; set; }

        public RenameColumnStatement() { }
        public RenameColumnStatement(estring tableName, estring columnName, estring newColumnName)
        {
            TableName = tableName;
            ColumnName = columnName;
            NewColumnName = newColumnName;
        }


        public override ISqlStatement Clone()
        {
            return new RenameColumnStatement() { TableName = TableName.Clone(), ColumnName = ColumnName.Clone(), NewColumnName = NewColumnName.Clone() };
        }
    }
}