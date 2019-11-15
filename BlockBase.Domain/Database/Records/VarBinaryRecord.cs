using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Records
{
    public class VarBinaryRecord : Record
    {
        public byte[] Value { get; set; }
        public VarBinaryRecord(string column, byte[] value, int bucketSize) : base(column, bucketSize)
        {
            Value = value;
            RecordType = 4;
        }
        public VarBinaryRecord(string column, byte[] value) : base(column)
        {
            Value = value;
            RecordType = 4;
        }
        public VarBinaryRecord() { RecordType = 4; }
        public override string GetValue()
        {
            return "0x" + BitConverter.ToString(Value).Replace("-", "");
        }

        public override string GetValueForDatabase()
        {
            return "0x" + BitConverter.ToString(Value).Replace("-", "");
        }
    }
}
