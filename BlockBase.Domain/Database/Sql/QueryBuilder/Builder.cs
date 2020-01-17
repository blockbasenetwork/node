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
        public IList<ISqlCommand> SqlCommands { get; private set; }


        public Builder()
        {
            SqlCommands = new List<ISqlCommand>();
        }

        public void AddStatement(ISqlStatement statement)
        {
            ISqlCommand sqlCommand;
            switch (statement) {

                case SimpleSelectStatement simpleSelectStatement:
                    sqlCommand = new ReadQuerySqlCommand(simpleSelectStatement);
                    break;
                case UpdateRecordStatement updateRecordStatement:
                    sqlCommand = new UpdateSqlCommand(updateRecordStatement);
                    break;
                case UseDatabaseStatement useDatabaseStatement:
                    sqlCommand = new DatabaseSqlCommand(useDatabaseStatement);
                    break;
                case DropDatabaseStatement dropDatabaseStatement:
                    sqlCommand = new DatabaseSqlCommand(dropDatabaseStatement);
                    break;
                case CreateDatabaseStatement createDatabaseStatement:
                    sqlCommand = new DatabaseSqlCommand(createDatabaseStatement);
                    break;
                case ListDatabasesStatement listDatabasesStatement:
                    sqlCommand = new ListOrDiscoverCurrentDatabaseCommand(listDatabasesStatement);
                    break;
                case CurrentDatabaseStatement currentDatabaseStatement:
                    sqlCommand = new ListOrDiscoverCurrentDatabaseCommand(currentDatabaseStatement);
                    break;
                default:
                    sqlCommand = new GenericSqlCommand(statement);
                    break;
            }
            SqlCommands.Add(sqlCommand);
        }

        public void AddStatements(IList<ISqlStatement> statements)
        {
            foreach (var statement in statements) AddStatement(statement);

        }

        public void BuildSqlStatements(IGenerator generator)
        {
            foreach (var command in SqlCommands)
            {
                var statements = command.TransformedSqlStatement;
                var transformedStatementsText = new List<string>();

                foreach (var statement in statements)
                {
                    var statementText = "";
                    switch (statement)
                    {
                        case CreateTableStatement createTableStatement:
                            statementText = generator.BuildString(createTableStatement);
                            break;

                        case AbstractAlterTableStatement abstractAlterTableStatement:
                            statementText = generator.BuildString(abstractAlterTableStatement);
                            break;

                        case DropTableStatement dropTableStatement:
                            statementText = generator.BuildString(dropTableStatement);
                            break;

                        case InsertRecordStatement insertRecordStatement:
                            statementText = generator.BuildString(insertRecordStatement);
                            break;

                        case UpdateRecordStatement updateRecordStatement:
                            statementText = generator.BuildString(updateRecordStatement);
                            break;

                        case DeleteRecordStatement deleteRecordStatement:
                            statementText = generator.BuildString(deleteRecordStatement);
                            break;

                        case SimpleSelectStatement simpleSelectStatement:
                            statementText = generator.BuildString(simpleSelectStatement);
                            break;

                        case CreateDatabaseStatement createDatabaseStatement:
                            statementText = generator.BuildString(createDatabaseStatement);
                            break;

                        case DropDatabaseStatement dropDatabaseStatement:
                            statementText = generator.BuildString(dropDatabaseStatement);
                            break;

                        case UseDatabaseStatement useDatabaseStatement:
                            statementText = generator.BuildString(useDatabaseStatement);
                            break;
                    }
                    transformedStatementsText.Add(statementText + ";");


                }
                command.TransformedSqlStatementText = transformedStatementsText;
            }
        }
    }
}