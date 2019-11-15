using BlockBase.Domain.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database
{
    public class Table
    {
        public string TableName { get; set; }
        public List<Column> Columns { get; set; }
        public Table(string tableName, List<Column> columns)
        {
            TableName = tableName;

            Columns = columns;
        }
    }
}
