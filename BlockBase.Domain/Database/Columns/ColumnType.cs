using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Columns
{
    public enum ColumnType
    {
        PrimaryColumn = 1,
        ForeignColumn = 2,
        UniqueColumn = 3,
        RangeColumn = 4,
        NormalColumn = 5
    }
}
