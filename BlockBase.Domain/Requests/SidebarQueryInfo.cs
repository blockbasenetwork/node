using System.Collections.Generic;

namespace BlockBase.Domain.Requests
{
    public class SidebarQueryInfo
    {
        public bool Encrypted { get; set; }
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
    }
}