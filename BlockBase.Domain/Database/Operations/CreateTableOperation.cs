using BlockBase.Domain.Database;
using BlockBase.Domain.Database.Columns;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace BlockBase.Domain.Database.Operations
{
    public class CreateTableOperation : ISqlOperation
    {
        public Table Table { get; set; }
        public CreateTableOperation()
        {
            
        }

        public string GetSQLQuery()
        {
            StringBuilder stringBuilder = new StringBuilder("CREATE TABLE " + Table.TableName + " ( ");
            using (IEnumerator<Column> columnEnumerator = Table.Columns.GetEnumerator())
            {
                int size = Table.Columns.Count;
                Column column;
                for (int i = 0; i < size; i++)
                {
                    columnEnumerator.MoveNext();
                    column = columnEnumerator.Current;
                    if(column.Type == SqlDbType.VarBinary) stringBuilder.Append(column.Name + " varbinary(" + column.Size + ")");
                    
                    else  stringBuilder.Append(column.Name + " " + column.Type.ToString());

                    if (column.NotNull) stringBuilder.Append(" NOT NULL");

                    if (column is PrimaryColumn) stringBuilder.Append(" PRIMARY KEY");
                    
                    if(column is UniqueColumn) stringBuilder.Append(" UNIQUE ");
                    
                    if (column is ForeignColumn foreignColumn) stringBuilder.Append(" FOREIGN KEY REFERENCES " + foreignColumn.ForeignTableName + "(" + foreignColumn.ForeignColumnName + ")");
                    
                    stringBuilder.Append(" ,");
                }
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }
            stringBuilder.Append(");");
            return stringBuilder.ToString();
        }

        public string GetMySQLQuery()
        {
            StringBuilder stringBuilder = new StringBuilder("CREATE TABLE " + Table.TableName + " ( ");
            using (IEnumerator<Column> columnEnumerator = Table.Columns.GetEnumerator())
            {
                int size = Table.Columns.Count;
                Column column;
                for (int i = 0; i < size; i++)
                {
                    columnEnumerator.MoveNext();
                    column = columnEnumerator.Current;
                    if (column.Type == SqlDbType.VarBinary)  stringBuilder.Append(column.Name + " varbinary(" + column.Size + ")");
                    
                    else if (column.Type == SqlDbType.UniqueIdentifier) stringBuilder.Append(column.Name + " char(38)");
                    
                    else stringBuilder.Append(column.Name + " " + column.Type.ToString());

                    if (column.NotNull) stringBuilder.Append(" NOT NULL");

                    if (column is PrimaryColumn) stringBuilder.Append(" PRIMARY KEY");
                    
                    if (column is UniqueColumn) stringBuilder.Append(" UNIQUE ");
                    
                    if (column is ForeignColumn foreignColumn) stringBuilder.Append(", FOREIGN KEY ("+ column.Name + ") REFERENCES " + foreignColumn.ForeignTableName + "(" + foreignColumn.ForeignColumnName + ")");

                    stringBuilder.Append(" ,");
                }
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }
            stringBuilder.Append(");");
            return stringBuilder.ToString();
        }
    }
}
