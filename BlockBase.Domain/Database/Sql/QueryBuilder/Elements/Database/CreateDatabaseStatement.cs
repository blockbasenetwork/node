using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database
{
    public class CreateDatabaseStatement : ISqlDatabaseStatement
    {
        public estring DatabaseName { get; set; }

        public CreateDatabaseStatement() { }

        public CreateDatabaseStatement(estring databaseName)
        {
            DatabaseName = databaseName;
        }

        public ISqlStatement Clone()
        {
            return new CreateDatabaseStatement() { DatabaseName = DatabaseName.Clone() };
        }

        public string GetStatementType()
        {
            return "create database";
        }
    }
}
