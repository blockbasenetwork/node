using System.Collections.Generic;

namespace BlockBase.Domain.Database.Sql.Response.Structure
{
    public class DatabaseInfo
    {
        public string DatabaseName { get; set; }
        public List<TableInfo> Tables { get; set; }
    }
}