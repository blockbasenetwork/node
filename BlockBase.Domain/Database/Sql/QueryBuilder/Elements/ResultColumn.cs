using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements
{
    public class ResultColumn
    {
        public estring TableName { get; set; }
        public estring ColumnName { get; set; }
        public bool AllColumnsfFlag { get; set; }

        public ResultColumn Clone()
        {
            return new ResultColumn() { TableName = TableName.Clone(), ColumnName = ColumnName.Clone(), AllColumnsfFlag = AllColumnsfFlag };
        }
    }
}