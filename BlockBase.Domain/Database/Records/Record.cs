using BlockBase.Domain.Database.Columns;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Records
{
    abstract public class Record
    {
        public string Column {  get; set; }
        public int BucketSize {  get; set; }
        public int ValueMaxRange {  get; set; }
        public int RecordType { get; set; }
        public ColumnType Type { get; set; }

        public Record() { }

        public Record(string column,  int bucketSize)
        {
            Column = column;
            BucketSize = bucketSize;
        }
        public Record(string column)
        {
            Column = column;
        }
        abstract public string GetValue();

        abstract public string GetValueForDatabase();
    }
}
