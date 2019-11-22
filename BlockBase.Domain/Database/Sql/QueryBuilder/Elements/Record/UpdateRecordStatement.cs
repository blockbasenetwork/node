using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record
{
    public class UpdateRecordStatement : ISqlStatement
    {
        public estring TableName { get; set; }
        public Dictionary<estring, Value> ColumnNamesAndUpdateValues { get; set; }

        public AbstractExpression WhereClause { get; set; }

        public UpdateRecordStatement() { }

        public UpdateRecordStatement(estring tableName, Dictionary<estring, Value> columnNamesAndUpdateValues, AbstractExpression whereClause)
        {
            TableName = tableName;
            ColumnNamesAndUpdateValues = columnNamesAndUpdateValues;
            WhereClause = whereClause;
        }

        public ISqlStatement Clone()
        {
            var updateRecordStatementClone = new UpdateRecordStatement()
            {
                TableName = TableName.Clone(),
                WhereClause = WhereClause.Clone(),
                ColumnNamesAndUpdateValues = new Dictionary<estring, Value>()
            };

            foreach (var entry in ColumnNamesAndUpdateValues)
            {
                updateRecordStatementClone.ColumnNamesAndUpdateValues.Add(entry.Key.Clone(), entry.Value);
            }
            return updateRecordStatementClone;
        }
    }
}