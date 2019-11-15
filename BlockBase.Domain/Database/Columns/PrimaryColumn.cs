using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BlockBase.Domain.Database
{
    public class PrimaryColumn : Column
    {
        public PrimaryColumn(string name) : base(name, SqlDbType.UniqueIdentifier, false)
        {
            ColumnType = Columns.ColumnType.PrimaryColumn;
        }
        public PrimaryColumn() { }
    }
}
