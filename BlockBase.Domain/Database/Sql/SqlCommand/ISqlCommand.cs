using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public interface ISqlCommand
    {
        string EncryptedValue { get; set; }
    }
}
