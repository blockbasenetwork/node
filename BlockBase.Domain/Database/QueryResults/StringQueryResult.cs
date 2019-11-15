using BlockBase.Domain.Database.Columns;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database
{
    public class StringQueryResult : IResult
    {
        public string ColumnName { get; set; }
        public string TableName { get; set; }
        public List<string> Values { get; set; }
        public ColumnType Type { get; set; }

        public StringQueryResult()
        {
            Values = new List<string>();
        }
        public void AddSqlValue(object value)
        {
            Values.Add((string)value);
        }

        public void AddMySqlValue(object value)
        {
            Values.Add((string)value);
        }
    }
}
