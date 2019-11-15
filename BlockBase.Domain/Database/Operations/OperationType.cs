using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Operations
{
    public enum OperationType
    {
        CreateTable,
        CreateColumn,
        DeleteColumn,
        DeleteRecord,
        DeleteTable,
        InsertRecord,
        UpdateRecord
    }
}
