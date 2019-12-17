using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class RenameTableStatement : AbstractAlterTableStatement
    {
        public estring NewTableName { get; set; }

        public RenameTableStatement() { }

        public RenameTableStatement(estring tableName, estring newTableName)
        {
            TableName = tableName;
            NewTableName = newTableName;
        }

        public override ISqlStatement Clone()
        {
            return new RenameTableStatement() { TableName = TableName.Clone(), NewTableName = NewTableName.Clone() };
        }
    }
}