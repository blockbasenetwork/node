using BlockBase.Domain.Database;
using BlockBase.Domain.Database.Columns;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BlockBase.Domain.Database.Operations
{
    public class CreateColumnOperation : ISqlOperation
    {
        public string TableName { get; set; }
        public Column Column { get; set; }

        public CreateColumnOperation()
        {

        }
        public string GetSQLQuery()
        {
            StringBuilder stringBuilder;
            if(Column.Type == SqlDbType.VarBinary) stringBuilder = new StringBuilder("ALTER TABLE " + TableName + " ADD " + Column.Name + " varbinary(" + Column.Size + ") ");
            
            else stringBuilder = new StringBuilder("ALTER TABLE " + TableName + " ADD " + Column.Name + " " + Column.Type.ToString());
            
            if (Column.NotNull) stringBuilder.Append("NOT NULL");
            
            stringBuilder.Append(";");
            return stringBuilder.ToString();
        }

        public string GetMySQLQuery()
        {
            return GetSQLQuery();
        }
    }
}
