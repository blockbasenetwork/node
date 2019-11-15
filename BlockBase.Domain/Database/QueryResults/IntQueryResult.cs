using BlockBase.Domain.Database.Columns;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.QueryResults
{
    public class IntQueryResult : IResult
    {
        public List<int?> Values { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public ColumnType Type { get; set; }

        public IntQueryResult(string tableName, string columnName, ColumnType type)
        {
            Values = new List<int?>();
            TableName = tableName;
            ColumnName = columnName;
            Type = type;
        }

        public IntQueryResult()
        {
            Values = new List<int?>();
        }

        public void AddSqlValue(object value)
        {
            if (value is DBNull)
            {
                Values.Add(null);
            }
            else
            {
                Values.Add((int)value);
            }
        }

        public void AddMySqlValue(object value)
        {
            if (value is DBNull)
            {
                Values.Add(null);
            }
            else
            {
                Values.Add((int)value);
            }
        }
    }
}
