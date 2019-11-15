using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BlockBase.Domain.Database.Columns
{
    public class NormalColumn : Column
    {
        public int NumberOfBuckets { get; set; }
        public NormalColumn() { }
        public NormalColumn(string name , bool notNull, int numberOfBuckets) :base(name, SqlDbType.VarBinary, notNull)
        {
            NumberOfBuckets = numberOfBuckets;
            ColumnType = Columns.ColumnType.NormalColumn;
        }
        public NormalColumn(string name, bool notNull, int size, int numberOfBuckets) : base(name, SqlDbType.VarBinary, notNull,size)
        {
            NumberOfBuckets = numberOfBuckets;
            ColumnType = Columns.ColumnType.NormalColumn;
        }
    }
}
