using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.QueryResults
{
    public class NewWhereField
    {
        public string Column { get; set; }
        public string Table { get; set; }
        public string Value { get; set; }
        public WhereTypeEnum WhereType { get; set; }
    }
}
