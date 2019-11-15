using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database
{
    public class UseDatabaseStatement : ISqlStatement
    {
        public estring DatabaseName { get; set; }

        public ISqlStatement Clone()
        {
            return new UseDatabaseStatement() { DatabaseName = DatabaseName.Clone() };
        }
    }
}