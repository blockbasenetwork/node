using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Operations
{
    public class DeleteRecordOperation : ISqlOperation,ICloneable
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string Value { get; set; }
        public DeleteRecordOperation()
        {
        }

        public string GetSQLQuery()
        {
            StringBuilder stringBuilder = new StringBuilder("DELETE FROM " + TableName + " WHERE " + ColumnName + " = " + Value + ";");
            return stringBuilder.ToString();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public string GetMySQLQuery()
        {
            return GetSQLQuery();
        }
    }
}
