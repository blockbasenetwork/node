using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record
{
    public class InsertRecordStatement : ISqlStatement
    {
        public estring TableName { get; set; }
        public IDictionary<estring, IList<Value>> ValuesPerColumn { get; set; }

        public InsertRecordStatement() { }

        public InsertRecordStatement(estring tableName)
        {
            TableName = tableName;
            ValuesPerColumn = new Dictionary<estring, IList<Value>>();
        }


        public ISqlStatement Clone()
        {
            var valuesPerColumnClone = new Dictionary<estring, IList<Value>>();
            foreach (var keyPair in ValuesPerColumn) valuesPerColumnClone.Add(keyPair.Key, keyPair.Value.Select(v => v.Clone()).ToList());
            return new InsertRecordStatement() { TableName = TableName.Clone(), ValuesPerColumn = valuesPerColumnClone };
        }

        public string GetStatementType()
        {
            return "insert record";
        }
    }
}