using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using System.Collections.Generic;

namespace BlockBase.Domain.Database.Sql.QueryBuilder
{
    public class Builder
    {
        public IList<ISqlStatement> SqlStatements { get; private set; }


        public Builder()
        {
            SqlStatements = new List<ISqlStatement>();
        }

        public Builder AddStatement(ISqlStatement statement)
        {
            SqlStatements.Add(statement);
            return this;
        }

        public Builder AddStatements(IList<ISqlStatement> statements)
        {
            foreach (var statement in statements)
            {
                SqlStatements.Add(statement);
            }
            return this;
        }

        public IList<SqlCommand> BuildSqlStatements(IGenerator generator)
        {
            var sqlCommands = new List<SqlCommand>();

            foreach (var sqlStatement in SqlStatements)
            {
                var sqlString = "";
                var isDatabaseStatement = false;
                string databaseName = null;

                switch (sqlStatement)
                {
                    case CreateTableStatement createTableStatement:
                        sqlString = generator.BuildString(createTableStatement);
                        break;

                    case AbstractAlterTableStatement abstractAlterTableStatement:
                        sqlString = generator.BuildString(abstractAlterTableStatement);
                        break;

                    case DropTableStatement dropTableStatement:
                        sqlString = generator.BuildString(dropTableStatement);
                        break;

                    case InsertRecordStatement insertRecordStatement:
                        sqlString = generator.BuildString(insertRecordStatement);
                        break;

                    case UpdateRecordStatement updateRecordStatement:
                        sqlString = generator.BuildString(updateRecordStatement);
                        break;

                    case DeleteRecordStatement deleteRecordStatement:
                        sqlString = generator.BuildString(deleteRecordStatement);
                        break;

                    case SimpleSelectStatement simpleSelectStatement:
                        sqlString = generator.BuildString(simpleSelectStatement);
                        break;

                    case SelectCoreStatement selectCoreStatement:
                        sqlString = generator.BuildString(selectCoreStatement);
                        break;

                    case CreateDatabaseStatement createDatabaseStatement:
                        sqlString = generator.BuildString(createDatabaseStatement);
                        databaseName = createDatabaseStatement.DatabaseName.Value;
                        isDatabaseStatement = true;
                        break;

                    case DropDatabaseStatement dropDatabaseStatement:
                        sqlString = generator.BuildString(dropDatabaseStatement);
                        isDatabaseStatement = true;
                        break;

                    case UseDatabaseStatement useDatabaseStatement:
                        databaseName = useDatabaseStatement.DatabaseName.Value;
                        sqlString = generator.BuildString(useDatabaseStatement);
                        break;
                }
                sqlCommands.Add(
                    new SqlCommand()
                    {
                        Value = sqlString + ";",
                        IsDatabaseStatement = isDatabaseStatement,
                        DatabaseName = databaseName

                    });
            }
            return sqlCommands;
        }
    }
}