using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BlockBase.Domain.Database.Columns
{
    public class RangeColumn :Column
    {
        public int MaxRange { get; set; }
        public bool CanBeNegative { get; set; }
        public int NumberOfBuckets { get; set; }
        public RangeColumn(string name, bool notNull, int size, int numberOfBuckets, int maxRange,bool canBeNegative) : base(name, SqlDbType.VarBinary,  notNull,  size)
        {
            NumberOfBuckets = numberOfBuckets;
            MaxRange = maxRange;
            CanBeNegative = canBeNegative;
            ColumnType = Columns.ColumnType.RangeColumn;
        }
        public RangeColumn(string name, bool notNull, int size, int numberOfBuckets, int maxRange) : base(name, SqlDbType.VarBinary, notNull, size)
        {
            NumberOfBuckets = numberOfBuckets;
            MaxRange = maxRange;
            ColumnType = Columns.ColumnType.RangeColumn;
        }
        public RangeColumn() { }
    }
}
