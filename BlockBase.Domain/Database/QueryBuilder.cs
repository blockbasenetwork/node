using BlockBase.Domain.Database.QueryResults;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database
{
    public class QueryBuilder
    {
        public IList<SelectField> SelectColumns { get; set; }
        public HashSet<string> FromTables { get; set; }
        public IList<WhereField> WhereEqualFields { get; private set; }
        public IList<WhereField> WhereEqualFieldsWithDBFormat { get; private set; }
        public IList<WhereField> WhereHigherThanFields { get; private set; }
        public IList<WhereField> WhereLessThanFields { get; private set; }
        public IList<WhereField> WhereHigherOrEqualFields { get; private set; }
        public IList<WhereField> WhereLessOrEqualFields { get; private set; }
        private IList<WhereField> WhereEqualOrFields { get; set; }
        public IList<JoinField> JoinFields { get; set; }
        public QueryBuilder()
        {
            SelectColumns = new List<SelectField>();
            FromTables = new HashSet<string>();
            WhereEqualFields = new List<WhereField>();
            WhereEqualFieldsWithDBFormat = new List<WhereField>();
            WhereHigherThanFields = new List<WhereField>();
            WhereLessThanFields = new List<WhereField>();
            WhereEqualOrFields= new List<WhereField>();
            WhereHigherOrEqualFields = new List<WhereField>();
            WhereLessOrEqualFields = new List<WhereField>();
            JoinFields = new List<JoinField>();
        }
        public QueryBuilder Select(string column, string table)
        {
            SelectColumns.Add(new SelectField { Column = column, Table = table });
            FromTables.Add(table);
            return this;
        }
        public QueryBuilder Where(string table, string column, string value)
        {
            var newWhere = new WhereField { Table = table, Column = column, Value = "'" + value + "'" };
            WhereEqualFields.Add(new WhereField { Table = table, Column = column, Value = value });
            WhereEqualFieldsWithDBFormat.Add(newWhere);
            return this;
        }
        public QueryBuilder Where(string table, string column, Guid value)
        {
            var newWhere = new WhereField { Table = table, Column = column, Value = "'" + value.ToString() + "'" };
            WhereEqualFieldsWithDBFormat.Add(newWhere);
            return this;
        }
        public QueryBuilder Where(string table, string column, byte[] value)
        {
            var newWhere = new WhereField { Table = table, Column = column, Value = "0x" + BitConverter.ToString(value).Replace("-", "")  };
            WhereEqualFieldsWithDBFormat.Add(newWhere);
            return this;
        }
        public QueryBuilder Where(string table, string column, int value)
        {
            var newWhere = new WhereField { Table = table, Column = column, Value = value.ToString() };
            WhereEqualFieldsWithDBFormat.Add(newWhere);
            WhereEqualFields.Add(new WhereField { Table = table, Column = column, Value = value.ToString()});
            return this;
        }
        public QueryBuilder Join(string tableToJoin, string columnToJoin, string tableAlreadyJoin, string columnAlreadyJoin)
        {
            var newWhere = new JoinField { TableAlreadyJoined = tableAlreadyJoin, TableToJoin = tableToJoin,
                ColumnAlreadyJoined = columnAlreadyJoin, ColumnToJoin = columnToJoin};
            FromTables.Remove(tableToJoin); 
            JoinFields.Add(newWhere);
            return this;
        }
        public QueryBuilder WhereHigherThan(string table, string column, int value)
        {
            var newWhere = new WhereField { Table = table, Column = column, Value = value.ToString() };
            WhereHigherThanFields.Add(newWhere);
            return this;
        }
        public QueryBuilder WhereLessThan(string table, string column, int value)
        {
            var newWhere = new WhereField { Table = table, Column = column, Value = value.ToString() };
            WhereLessThanFields.Add(newWhere);
            return this;
        }
        public QueryBuilder WhereHigherOrEqual(string table, string column, int value)
        {
            var newWhere = new WhereField { Table = table, Column = column, Value = value.ToString() };
            WhereHigherOrEqualFields.Add(newWhere);
            return this;
        }
        public QueryBuilder WhereLessOrEqual(string table, string column, int value)
        {
            var newWhere = new WhereField { Table = table, Column = column, Value = value.ToString() };
            WhereLessOrEqualFields.Add(newWhere);
            return this;
        }
        public QueryBuilder WhereEqualOr(string table, string column, string value)
        {
            var newWhereField = new WhereField { Table = table, Column = column, Value = value };
            WhereEqualOrFields.Add(newWhereField);
            return this;
        }
        public string Build()
        {
            if (SelectColumns.Count == 0 || FromTables.Count == 0) return null;

            int lastPosition = SelectColumns.Count - 1;
            var stringBuilder = new StringBuilder("SELECT ");
            for (int i = 0; i < lastPosition; i++)
            {
                var field = SelectColumns[i];
                stringBuilder.Append(field.Table + "." + field.Column + ", ");
            }

            stringBuilder.Append(SelectColumns[lastPosition].Column + " ");
            stringBuilder.Append("FROM ");
            lastPosition = FromTables.Count - 1;
            IEnumerator<string> enumerator = FromTables.GetEnumerator();
            for (int i = 0; i < lastPosition; i++)
            {
                enumerator.MoveNext();
                stringBuilder.Append(enumerator.Current + ", ");
            }
            enumerator.MoveNext();
            stringBuilder.Append(enumerator.Current + " ");

            foreach( JoinField joinField in JoinFields) // JOINS
                stringBuilder.Append(" INNER JOIN " + joinField.TableToJoin + " ON " + joinField.TableToJoin + "." + joinField.ColumnToJoin
                     + " = " + joinField.TableAlreadyJoined + "." + joinField.ColumnAlreadyJoined);
            
            if(WhereEqualFieldsWithDBFormat.Count > 0 || WhereEqualOrFields.Count > 0) stringBuilder.Append(" WHERE "); // WHERE

            if (WhereEqualFieldsWithDBFormat.Count > 0)
            {
                lastPosition = WhereEqualFieldsWithDBFormat.Count - 1;
                for (int i = 0; i < lastPosition; i++)
                {
                    var field = WhereEqualFieldsWithDBFormat[i];
                    stringBuilder.Append(field.Table + "." + field.Column + "=" + field.Value + " AND ");
                }
                stringBuilder.Append(WhereEqualFieldsWithDBFormat[lastPosition].Table + "." + WhereEqualFieldsWithDBFormat[lastPosition].Column
                     + "=" + WhereEqualFieldsWithDBFormat[lastPosition].Value);
            }
            if (WhereEqualOrFields.Count > 0)
            {
                lastPosition = WhereEqualOrFields.Count - 1;
                for (int i = 0; i < lastPosition; i++)
                {
                    var field = WhereEqualOrFields[i];
                    stringBuilder.Append(field.Table + "." + field.Column + " = " + field.Value + " OR "); 
                }
                stringBuilder.Append(WhereEqualOrFields[lastPosition].Table + "." + WhereEqualOrFields[lastPosition].Column + " = "
                     + WhereEqualOrFields[lastPosition].Value);
            }
            return stringBuilder.ToString();
        }
    }
}
