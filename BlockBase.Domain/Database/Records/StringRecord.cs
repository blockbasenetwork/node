using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Records
{
    public class StringRecord : Record
    {
        public StringRecord() { RecordType = 3; }
        public StringRecord(string column,string value, int bucketSize) : base(column,bucketSize)
        {
            Value = value;
            RecordType = 3;
        }
        public StringRecord(string column, string value) : base(column)
        {
            Value = value;
            RecordType = 3;
        }
        public string Value {  get; set; }
        public override string GetValue()
        {
            return  Value ;
        }

        public override string GetValueForDatabase()
        {
            return "'" + Value + "'";
        }
    }
}
