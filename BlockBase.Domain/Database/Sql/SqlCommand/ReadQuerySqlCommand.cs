using System;
using System.Collections.Generic;
using System.Text;
using BlockBase.Domain.Database.Sql.QueryBuilder;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class ReadQuerySqlCommand : ISqlCommand
    {
        public string EncryptedValue { get; set; }

        public Builder PlainBuilder { get; set; }

        public ReadQuerySqlCommand(string encryptedValue)
        {
            EncryptedValue = encryptedValue;
        }
    }
}
