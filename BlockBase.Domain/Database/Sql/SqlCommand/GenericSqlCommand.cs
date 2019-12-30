using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class GenericSqlCommand : ISqlCommand
    {
        public string EncryptedValue { get; set; }

        public GenericSqlCommand(string encryptedValue)
        {
            EncryptedValue = encryptedValue;
        }
    }
}
