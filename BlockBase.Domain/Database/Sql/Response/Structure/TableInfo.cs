using System.Collections.Generic;

namespace BlockBase.Domain.Database.Sql.Response.Structure
{
    public class TableInfo
    {
        public string TableName { get; set; }
        public IList<ColumnInfo> Columns { get; set; }
    }
}