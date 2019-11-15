namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class Table : IDataSource
    {
        public estring DatabaseName { get; set; }
        public estring TableName { get; set; }
    }
}