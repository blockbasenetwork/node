using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.QueryResults
{
    public class JoinField
    {
        public string TableToJoin { get; set; }
        public string ColumnToJoin { get; set; }
        public string TableAlreadyJoined { get; set; }
        public string ColumnAlreadyJoined { get; set; }

    }
}
