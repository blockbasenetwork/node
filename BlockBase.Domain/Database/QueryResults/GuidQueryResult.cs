using BlockBase.Domain.Database.Columns;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database
{
    public class GuidQueryResult : IResult
    {

        public List<string> Values { get; set; }
        public string TableName { get ; set; }
        public string ColumnName { get; set; }
        public ColumnType Type { get; set; }

        public GuidQueryResult(string tableName, string columnName, ColumnType type)
        {
            Values = new List<string>();
            TableName = tableName;
            ColumnName = columnName;
            Type = type;
        }
        public GuidQueryResult()
        {
            Values = new List<string>();

        }
        public void AddSqlValue(object value)
        {
            var guid = (Guid) value;
            Values.Add(guid.ToString());
        }

        public void AddMySqlValue(object value)
        {
            var guid = (string)value;
            Values.Add(guid);
        }
    }
}
