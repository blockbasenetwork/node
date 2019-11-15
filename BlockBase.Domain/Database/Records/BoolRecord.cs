using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Records
{
    public class BoolRecord : Record
    {
        public bool Value { get; set; }
        public BoolRecord() { RecordType = 0; }
        public BoolRecord(string column, bool value, int bucketSize) : base(column, bucketSize)
        {
            Value = value;
            RecordType = 0;
        }
        public BoolRecord(string column, bool value) : base(column)
        {
            Value = value;
            RecordType = 0;
        }
        public override string GetValue()
        {
            return Value.ToString();
        }
        public override string GetValueForDatabase()
        {
            return (Value ? 1 : 0).ToString() ;
        }

    }
}
