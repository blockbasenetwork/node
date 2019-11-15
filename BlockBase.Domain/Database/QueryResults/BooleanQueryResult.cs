using System;
using System.Collections.Generic;
using System.Text;
using BlockBase.Domain.Database.Columns;

namespace BlockBase.Domain.Database.QueryResults
{
    public class BooleanQueryResult : IResult
    {
        public List<bool> Values { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public ColumnType Type { get; set; }

        public BooleanQueryResult(string tableName, string columnName, ColumnType type)
        {
            TableName = tableName;
            ColumnName = columnName;
            Values = new List<bool>();
            Type = type;
        }
        public BooleanQueryResult(string tableName, string columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
            Values = new List<bool>();
        }
        public BooleanQueryResult()
        {
            Values = new List<bool>();
        }
        public void AddSqlValue(object value)
        {
            Values.Add((bool)value);
        }

        public void AddMySqlValue(object value)
        {
            var intValue = (UInt64)value;
            
            Values.Add((intValue == 1 ? true : false));
        }
    }
}
