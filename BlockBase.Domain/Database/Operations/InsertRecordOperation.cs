using BlockBase.Domain.Database.Records;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Operations
{
    public class InsertRecordOperation : ISqlOperation
    {
        public List<Record> ValuesToInsert { get; set; }

        public string TableName { get; set; }

        public InsertRecordOperation()
        {
          
        }


        public string GetSQLQuery()
        {
            StringBuilder stringBuilder = new StringBuilder("INSERT INTO " + TableName);
            StringBuilder columnsToInsertString = new StringBuilder("(");
            StringBuilder valuesToInsertString = new StringBuilder("(");
            foreach (var record in ValuesToInsert)
            {
                columnsToInsertString.Append(record.Column + ",");
                valuesToInsertString.Append(record.GetValueForDatabase() + ",");
            }
            columnsToInsertString.Remove(columnsToInsertString.Length - 1, 1);
            valuesToInsertString.Remove(valuesToInsertString.Length - 1, 1);

            columnsToInsertString.Append(")");
            valuesToInsertString.Append(")");

            stringBuilder.Append(columnsToInsertString);
            stringBuilder.Append(" VALUES ");
            stringBuilder.Append(valuesToInsertString);

            return stringBuilder.ToString();
        }

        public string GetMySQLQuery()
        {
            return GetSQLQuery();
        }
    }
}
