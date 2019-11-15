using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Records
{
    public class IntRecord : Record
    {
        public int Value {  get; set; }
        public IntRecord()
        {
            RecordType = 2;
        }
        public IntRecord(string column, int value, int bucketSize) : base(column, bucketSize)
        {
            Value = value;
            RecordType = 2;
        }
        public IntRecord(string column, int value) : base(column)
        {
            Value = value;
            RecordType = 2;
        }
        public override string GetValue()
        {
            return Value.ToString();
        }

        public override string GetValueForDatabase()
        {
            return Value.ToString();
        }
    }
}
