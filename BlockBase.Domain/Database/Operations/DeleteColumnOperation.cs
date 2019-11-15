using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Operations
{
    public class DeleteColumnOperation : ISqlOperation
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }

        public DeleteColumnOperation()
        {
         
        }

        public string GetSQLQuery()
        {
            StringBuilder stringBuilder = new StringBuilder("ALTER TABLE " + TableName + " DROP COLUMN " + ColumnName + ";");

            return stringBuilder.ToString();
        }

        public string GetMySQLQuery()
        {
            return GetSQLQuery();
        }
    }
}
