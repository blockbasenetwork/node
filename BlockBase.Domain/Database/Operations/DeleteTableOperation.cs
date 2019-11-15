using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Operations
{
    public class DeleteTableOperation : ISqlOperation
    {
        public string TableName { get; set; }

        public DeleteTableOperation()
        {
            
        }

        public string GetSQLQuery()
        {
            StringBuilder stringBuilder = new StringBuilder("DROP TABLE " + TableName);

            return stringBuilder.ToString();
        }

        public string GetMySQLQuery()
        {
            return GetSQLQuery();
        }
    }
}
