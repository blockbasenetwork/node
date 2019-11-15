using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class CreateTableStatement : ISqlStatement
    {
        public estring TableName { get; set; }
        public IList<ColumnDefinition> ColumnDefinitions { get; set; }

        public ISqlStatement Clone()
        {
            return new CreateTableStatement() { TableName = TableName.Clone(), ColumnDefinitions = ColumnDefinitions.Select(c => c.Clone()).ToList() };
        }
    }
}