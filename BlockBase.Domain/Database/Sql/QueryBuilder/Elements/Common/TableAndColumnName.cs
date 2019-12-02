using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class TableAndColumnName
    {
        public estring TableName { get; set; }
        public estring ColumnName { get; set; }

        public TableAndColumnName() { }

        public TableAndColumnName(estring tableName, estring columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        public string GetFinalString()
        {
            return TableName.GetFinalString() + "." + ColumnName.GetFinalString();
        }

        public TableAndColumnName Clone()
        {
            return new TableAndColumnName(TableName = TableName.Clone(), ColumnName = ColumnName.Clone());
        }
    }

    
}
