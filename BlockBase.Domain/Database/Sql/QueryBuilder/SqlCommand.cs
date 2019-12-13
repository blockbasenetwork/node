using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder
{
    public class SqlCommand
    {
        public string Value { get; set; }
        public bool IsDatabaseStatement { get; set; }
        public string DatabaseName { get; set; }
    }
}
