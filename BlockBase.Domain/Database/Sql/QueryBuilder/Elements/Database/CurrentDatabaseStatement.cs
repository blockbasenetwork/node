﻿using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database
{
    public class CurrentDatabaseStatement : ISqlStatement
    {
        public ISqlStatement Clone()
        {
            throw new System.NotImplementedException();
        }

        public string GetStatementType()
        {
            return "current database";
        }
    }
}