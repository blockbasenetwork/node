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
        public Dictionary<estring, IList<ISqlStatement>> SqlStatementsPerDatabase { get; private set; }


        public Builder()
        {
            SqlStatementsPerDatabase = new Dictionary<estring, IList<ISqlStatement>>();
        }

        public Builder Clone()
        {
            var builderClone = new Builder();

            builderClone.SqlStatementsPerDatabase = new Dictionary<estring, IList<ISqlStatement>>();

            foreach (var entry in SqlStatementsPerDatabase)
            {
                builderClone.SqlStatementsPerDatabase.Add(entry.Key.Clone(), new List<ISqlStatement>());
                foreach (var sqlStatement in entry.Value)
                {
                    builderClone.SqlStatementsPerDatabase[entry.Key].Add(sqlStatement.Clone());
                }
            }

            return builderClone;
        }

        // public SimpleSelectStatement AddSimpleSelectStatement(SimpleSelectStatement statment)
        // {
        //     _sqlStatements.Add(statment);
        //     return statment;
        // }

        public Builder AddStatement(ISqlStatement statment, estring databaseName)
        {
            if (!SqlStatementsPerDatabase.ContainsKey(databaseName))
                SqlStatementsPerDatabase.Add(databaseName, new List<ISqlStatement>());
            SqlStatementsPerDatabase[databaseName].Add(statment);
            return this;
        }



        public Dictionary<string, IList<SqlCommand>> BuildQueryStrings(IGenerator generator)
        {
            var sqlCommandsPerDatabase = new Dictionary<string, IList<SqlCommand>>();

            foreach (var keyValue in SqlStatementsPerDatabase)
            {
                var sqlCommands = new List<SqlCommand>();

                foreach (var sqlStatement in SqlStatementsPerDatabase[keyValue.Key])
                {
                    var sqlString = "";
                    var isDatabaseStatement = false;

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
                            isDatabaseStatement = true;
                            break;

                        case DropDatabaseStatement dropDatabaseStatement:
                            sqlString = generator.BuildString(dropDatabaseStatement);
                            isDatabaseStatement = true;
                            break;

                        case UseDatabaseStatement useDatabaseStatement:
                            sqlString = generator.BuildString(useDatabaseStatement);
                            isDatabaseStatement = true;
                            break;
                    }
                    sqlCommands.Add(
                        new SqlCommand()
                        {
                            Value = sqlString + ";",
                            IsDatabaseStatement = isDatabaseStatement

                        });
                }
                sqlCommandsPerDatabase.Add(keyValue.Key.GetFinalString(), sqlCommands);
            }
            return sqlCommandsPerDatabase;
        }
    }
}