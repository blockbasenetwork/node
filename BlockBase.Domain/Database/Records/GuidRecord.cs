using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Records
{
    public class GuidRecord : Record
    {
        public Guid Value { get; set; }
        public GuidRecord() { RecordType = 1; }
        public GuidRecord(string column, Guid value, int bucketSize) : base(column, bucketSize)
        {
            Value = value;
            RecordType = 1;
        }
        public GuidRecord(string column, Guid value) : base(column)
        {
            Value = value;
            RecordType = 1;
        }
        public override string GetValue()
        {
            return  Value.ToString();
        }

        public override string GetValueForDatabase()
        {
            return "'" + Value.ToString() + "'";
        }
    }
}
