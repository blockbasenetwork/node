using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.SqlCommand;
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

        public IList<ISqlCommand> BuildSqlStatements(IGenerator generator)
        {
            var sqlCommands = new List<ISqlCommand>();

            foreach (var sqlStatement in SqlStatements)
            {
                ISqlCommand command = null;


                switch (sqlStatement)
                {
                    case CreateTableStatement createTableStatement:
                        command = new GenericSqlCommand(generator.BuildString(createTableStatement));
                        break;

                    case AbstractAlterTableStatement abstractAlterTableStatement:
                        command = new GenericSqlCommand(generator.BuildString(abstractAlterTableStatement));
                        break;

                    case DropTableStatement dropTableStatement:
                        command = new GenericSqlCommand(generator.BuildString(dropTableStatement));
                        break;

                    case InsertRecordStatement insertRecordStatement:
                        command = new GenericSqlCommand(generator.BuildString(insertRecordStatement));
                        break;

                    case UpdateRecordStatement updateRecordStatement:
                        command = new GenericSqlCommand(generator.BuildString(updateRecordStatement));
                        break;

                    case DeleteRecordStatement deleteRecordStatement:
                        command = new GenericSqlCommand(generator.BuildString(deleteRecordStatement));
                        break;

                    case SimpleSelectStatement simpleSelectStatement:
                        command = new ReadQuerySqlCommand(generator.BuildString(simpleSelectStatement));
                        break;

                    case CreateDatabaseStatement createDatabaseStatement:
                        command = new DatabaseSqlCommand(generator.BuildString(createDatabaseStatement), createDatabaseStatement.DatabaseName.Value);
                        break;

                    case DropDatabaseStatement dropDatabaseStatement:
                        command = new DatabaseSqlCommand(generator.BuildString(dropDatabaseStatement));
                        break;

                    case UseDatabaseStatement useDatabaseStatement:
                        command = new DatabaseSqlCommand(generator.BuildString(useDatabaseStatement), useDatabaseStatement.DatabaseName.Value);
                        break;
                }
                sqlCommands.Add(command);
            }
            return sqlCommands;
        }
    }
}