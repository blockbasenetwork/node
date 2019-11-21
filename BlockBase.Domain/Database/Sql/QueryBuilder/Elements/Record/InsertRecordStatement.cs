using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record
{
    public class InsertRecordStatement : ISqlStatement
    {
        public estring TableName { get; set; }
        public IDictionary<estring, IList<string>> ValuesPerColumn { get; set; }

        public InsertRecordStatement() { }

        public InsertRecordStatement(estring tableName)
        {
            TableName = tableName;
            ValuesPerColumn = new Dictionary<estring, IList<string>>();
        }


        public ISqlStatement Clone()
        {
            //TODO: clone dictionary elements
            return new InsertRecordStatement() { TableName = TableName.Clone(), ValuesPerColumn = new Dictionary<estring, IList<string>>(ValuesPerColumn) };
        }
    }
}