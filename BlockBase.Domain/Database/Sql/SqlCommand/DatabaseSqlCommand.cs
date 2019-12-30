using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class DatabaseSqlCommand : ISqlCommand
    {
        public string EncryptedValue { get; set; }
        public string DatabaseName { get; set; }

        public DatabaseSqlCommand(string encryptedValue, string databaseName = null)
        {
            EncryptedValue = encryptedValue;
            DatabaseName = databaseName;
        }
    }
}
