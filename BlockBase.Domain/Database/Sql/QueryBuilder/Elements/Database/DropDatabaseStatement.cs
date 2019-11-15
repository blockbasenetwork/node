using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database
{
    public class DropDatabaseStatement : ISqlStatement
    {
        public estring DatabaseName { get; set; }

        public ISqlStatement Clone()
        {
            return new DropDatabaseStatement() { DatabaseName = DatabaseName.Clone() };
        }
    }
}
