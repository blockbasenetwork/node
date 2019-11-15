using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BlockBase.Domain.Database.Columns
{
    public class UniqueColumn : Column
    {
        public UniqueColumn(string name, SqlDbType type, bool notNull) : base(name, type, notNull)
        {
            ColumnType = Columns.ColumnType.UniqueColumn;
        }
        public UniqueColumn(string name, SqlDbType type, bool notNull, int size) : base(name, type, notNull, size)
        {
            ColumnType = Columns.ColumnType.UniqueColumn;
        }
        public UniqueColumn() { }
    }
}
