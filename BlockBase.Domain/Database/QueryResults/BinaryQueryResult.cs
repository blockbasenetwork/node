using BlockBase.Domain.Database.Columns;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database
{
    public class BinaryQueryResult : IResult
    {
        public List<byte[]> Values { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public ColumnType Type { get; set; }

        public BinaryQueryResult(string tableName, string columnName, ColumnType type)
        {
            TableName = tableName;
            ColumnName = columnName;
            Values = new List<byte[]>();
            Type = type;
        }
        public BinaryQueryResult(string tableName, string columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
            Values = new List<byte[]>();
        }
        public BinaryQueryResult()
        {
            Values = new List<byte[]>();
        }

        public void AddSqlValue(object value)
        {
            if (value is DBNull)
            {
                Values.Add(null);
            }
            else
            {
                Values.Add((byte[])value);
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
                Values.Add((byte[])value);
            }
        }
    }
}
