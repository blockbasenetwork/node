using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Operations
{
    public interface ISqlOperation
    {
        string GetSQLQuery();
        string GetMySQLQuery();
    }
}
