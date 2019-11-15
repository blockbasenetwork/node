using BlockBase.Domain.Database.Records;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Operations
{
    public class UpdateRecordOperation : ISqlOperation,ICloneable
    {
        public string TableName { get; set; }
        public string IdentifierColumn { get; set; }
        public string IdentifierValue { get; set;}
        
        public List<Record> ValuesToUpdate { get; set; }
        public UpdateRecordOperation()
        {
            
        }

        public string GetSQLQuery()
        {
            StringBuilder stringBuilder = new StringBuilder("UPDATE " + TableName + " SET ");
            foreach (var record in ValuesToUpdate)
            {
                stringBuilder.Append(record.Column + " = " + record.GetValueForDatabase() + " ,");
            }
            stringBuilder.Remove(stringBuilder.Length - 1, 1);
            stringBuilder.Append("WHERE " + IdentifierColumn + "= '" + IdentifierValue + "'");
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
