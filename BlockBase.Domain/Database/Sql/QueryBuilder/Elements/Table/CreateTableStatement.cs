using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class CreateTableStatement : ISqlStatement
    {
        public estring TableName { get; set; }
        public IList<ColumnDefinition> ColumnDefinitions { get; set; }

        public CreateTableStatement()
        {
            ColumnDefinitions = new List<ColumnDefinition>();
        }

        public CreateTableStatement(estring tableName)
        {
            TableName = tableName;
            ColumnDefinitions = new List<ColumnDefinition>();
        }

        public ISqlStatement Clone()
        {
            return new CreateTableStatement() { TableName = TableName.Clone(), ColumnDefinitions = ColumnDefinitions.Select(c => c.Clone()).ToList() };
        }

        public string GetStatementType()
        {
            return "create table";
        }
    }
}