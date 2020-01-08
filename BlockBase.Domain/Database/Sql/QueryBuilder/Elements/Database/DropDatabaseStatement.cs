using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database
{
    public class DropDatabaseStatement : ISqlDatabaseStatement
    {
        public estring DatabaseName { get; set; }

        public DropDatabaseStatement() { }
        public DropDatabaseStatement(estring databaseName)
        {
            DatabaseName = databaseName;
        }

        public ISqlStatement Clone()
        {
            return new DropDatabaseStatement() { DatabaseName = DatabaseName.Clone() };
        }
    }
}
