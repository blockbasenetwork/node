using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class AddColumnStatement : AbstractAlterTableStatement
    {
        public ColumnDefinition ColumnDefinition { get; set; }

        public AddColumnStatement() { }

        public AddColumnStatement(estring tableName, ColumnDefinition columnDefinition)
        {
            TableName = tableName;
            ColumnDefinition = columnDefinition;
        }

        public override ISqlStatement Clone()
        {
            return new AddColumnStatement() { ColumnDefinition = ColumnDefinition.Clone(), TableName = TableName.Clone() };
        }
    }
}