using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BlockBase.Domain.Database
{
    public class ForeignColumn : Column
    {
        public string ForeignColumnName { get; set; }
        public string ForeignTableName { get; set; }
        public ForeignColumn(string name, bool notNull, string foreignTable, string foreignColumn) : base (name, SqlDbType.UniqueIdentifier, notNull)
        {
            ForeignColumnName = foreignColumn;
            ForeignTableName = foreignTable;
            ColumnType = Columns.ColumnType.ForeignColumn;
        }
        public ForeignColumn() { }
    }
}
