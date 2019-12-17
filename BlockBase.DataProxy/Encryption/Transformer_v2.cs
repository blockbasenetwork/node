using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using System;
using System.Collections.Generic;
using System.Text;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.ColumnConstraint;
using BlockBase.Domain.Database.Info;
using System.Linq;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.DataProxy.Encryption.Pocos;

namespace BlockBase.DataProxy.Encryption
{
    public class Transformer_v2
    {
        //TODO: refactor this to go to configs
        private static string _separatingChar = "_";
        private static string _ivPrefix = "iv";
        private static string _bucketPrefix = "bkt";
        private static string _equalityBucketPrefix = _bucketPrefix + "e";
        private static string _rangeBucketPrefix = _bucketPrefix + "r";

        private static readonly string ENCRYPTED_KEY_WORD = "encrypted";
        private static readonly string TEXT_KEY_WORD = "text";
        private static readonly string TRUE_KEY_WORD = "TRUE";
        private static readonly string FALSE_KEY_WORD = "FALSE";

        private static readonly estring INFO_TABLE_NAME = new estring(InfoTableConstants.INFO_TABLE_NAME);
        private static readonly estring NAME = new estring(InfoTableConstants.NAME);
        private static readonly estring DATA_ENCRYPTED = new estring(InfoTableConstants.IS_DATA_ENCRYPTED);
        private static readonly estring KEY_READ = new estring(InfoTableConstants.KEY_READ);
        private static readonly estring KEY_MANAGE = new estring(InfoTableConstants.KEY_MANAGE);
        private static readonly estring PARENT = new estring(InfoTableConstants.PARENT);
        private static readonly estring IV = new estring(InfoTableConstants.IV);

        private PSqlConnector _psqlConnector;
        private IEncryptor _encryptor;

        //TODO: this will not have a psql conector, instead it will have a classe that will communicate with the producer
        public Transformer_v2(PSqlConnector psqlConnector)
        {
            _psqlConnector = psqlConnector;
            //_encryptor = new Encryptor();
        }

        public Builder GetTransformedBuilder(Builder builder)
        {
            var transformedBuilder = new Builder();
            InfoRecord databaseInfoRecord = null;
            foreach (var sqlStatement in builder.SqlStatements)
            {
                switch (sqlStatement)
                {
                    case CreateDatabaseStatement createDatabaseStatement:
                        transformedBuilder.AddStatements(GetTransformedCreateDatabaseStatement(createDatabaseStatement));
                        break;

                    case DropDatabaseStatement dropDatabaseStatement:
                        transformedBuilder.AddStatements(GetTransformedDropDatabaseStatement(dropDatabaseStatement));
                        break;

                    case UseDatabaseStatement useDatabaseStatement:
                        transformedBuilder.AddStatement(GetTransformedUseDatabaseStatement(useDatabaseStatement, out databaseInfoRecord));
                        break;

                    case CreateTableStatement createTableStatement:
                        transformedBuilder.AddStatements(GetTransformedCreateTableStatement(createTableStatement, databaseInfoRecord.IV));
                        break;

                    case DropTableStatement dropTableStatement:
                        transformedBuilder.AddStatements(GetTransformedDropTableStatement(dropTableStatement, databaseInfoRecord.IV));
                        break;

                    case AbstractAlterTableStatement abstractAlterTableStatement:
                        transformedBuilder.AddStatements(GetTransformedAlterTableStatement(abstractAlterTableStatement, databaseInfoRecord.IV));
                        break;

                    case InsertRecordStatement insertRecordStatement:
                        transformedBuilder.AddStatement(GetTransformedInsertRecordStatement(insertRecordStatement, databaseInfoRecord.IV));
                        break;


                }
            }
            return transformedBuilder;
        }
        #region Transform SqlStatements
        private List<ISqlStatement> GetTransformedCreateDatabaseStatement(CreateDatabaseStatement createDatabaseStatement)
        {
            var datatabaseInfoRecord = _encryptor.CreateInfoRecord(createDatabaseStatement.DatabaseName, null);

            return new List<ISqlStatement>()
            {
                new CreateDatabaseStatement(new estring(datatabaseInfoRecord.Name)),
                CreateInfoTable(),
                CreateInsertRecordStatementForInfoTable(datatabaseInfoRecord)
            };
        }
        private List<ISqlStatement> GetTransformedDropDatabaseStatement(DropDatabaseStatement dropDatabaseStatement)
        {
            var infoRecord = _encryptor.RemoveInfoRecord(dropDatabaseStatement.DatabaseName, null);
            return new List<ISqlStatement>()
            {
                CreateDeleteRecordStatementForInfoTable(infoRecord.IV),
                new DropDatabaseStatement(new estring(infoRecord.Name))
            };
        }
        private UseDatabaseStatement GetTransformedUseDatabaseStatement(UseDatabaseStatement useDatabaseStatement, out InfoRecord databaseInfoRecord)
        {
            databaseInfoRecord = _encryptor.FindInfoRecord(useDatabaseStatement.DatabaseName, null);
            return new UseDatabaseStatement(new estring(databaseInfoRecord.Name));
        }

        private IList<ISqlStatement> GetTransformedCreateTableStatement(CreateTableStatement createTableStatement, string databaseIV)
        {
            var tableInfoRecord = _encryptor.CreateInfoRecord(createTableStatement.TableName, databaseIV);
            var transformedCreateTableStatement = new CreateTableStatement(new estring(tableInfoRecord.Name));

            var transformedStatements = new List<ISqlStatement>()
            {
                transformedCreateTableStatement,
                CreateInsertRecordStatementForInfoTable(tableInfoRecord)
            };

            foreach (var columnDef in createTableStatement.ColumnDefinitions)
            {
                var additionalColAndInfoInserts = GetTransformedColumnDefinition(columnDef, tableInfoRecord.IV, databaseIV);

                ((List<ColumnDefinition>)createTableStatement.ColumnDefinitions).AddRange(additionalColAndInfoInserts.Item1);
                transformedStatements.AddRange(additionalColAndInfoInserts.Item2);
            }

            return transformedStatements;
        }
        private IList<ISqlStatement> GetTransformedDropTableStatement(DropTableStatement dropTableStatement, string databaseName)
        {
            var sqlStatements = new List<ISqlStatement>();
            var tableInfoRecord = _encryptor.RemoveInfoRecord(dropTableStatement.TableName, databaseName);
            sqlStatements.Add(new DropTableStatement(new estring(tableInfoRecord.Name)));
            sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(tableInfoRecord.IV));
            return sqlStatements;
        }

        private IList<ISqlStatement> GetTransformedAlterTableStatement(AbstractAlterTableStatement alterTableStatement, string databaseIV)
        {
            switch (alterTableStatement)
            {
                case RenameTableStatement renameTableStatement:
                    return new List<ISqlStatement>(GetTransformedRenameTableStatement(renameTableStatement, databaseIV));

                case RenameColumnStatement renameColumnStatement:
                    return new List<ISqlStatement>(GetTransformedRenameColumnStatement(renameColumnStatement, databaseIV));

                case AddColumnStatement addColumnStatement:
                    return new List<ISqlStatement>(GetTransformedAddColumnStatement(addColumnStatement, databaseIV));

                case DropColumnStatement dropColumnStatement:
                    return new List<ISqlStatement>(GetTransformedDropColumnStatement(dropColumnStatement, databaseIV));
            }

            throw new FormatException("Alter table statement type not recognized.");
        }

        private IList<ISqlStatement> GetTransformedRenameTableStatement(RenameTableStatement renameTableStatement, string databaseIV)
        {
            var sqlStatements = new List<ISqlStatement>();

            var changeInfoRecordResults = _encryptor.ChangeInfoRecord(renameTableStatement.TableName, renameTableStatement.NewTableName, databaseIV);
            var transformedOldTableName = changeInfoRecordResults.Item1;
            var transformedNewTableName = changeInfoRecordResults.Item2;

            sqlStatements.Add(new RenameTableStatement( new estring(transformedOldTableName), new estring(transformedNewTableName)));

            sqlStatements.Add(
                new UpdateRecordStatement(
                    INFO_TABLE_NAME,
                    new Dictionary<estring, Value>() { { NAME, new Value(transformedNewTableName, true) } },
                    new ComparisonExpression(
                        INFO_TABLE_NAME,
                        NAME,
                        new Value(transformedOldTableName, true),
                        ComparisonExpression.ComparisonOperatorEnum.Equal)
                    ));

            sqlStatements.Add(
                new UpdateRecordStatement(
                    INFO_TABLE_NAME,
                    new Dictionary<estring, Value>() { { PARENT, new Value(transformedNewTableName, true) } },
                    new ComparisonExpression(
                        INFO_TABLE_NAME, 
                        PARENT, 
                        new Value(transformedOldTableName, true), 
                        ComparisonExpression.ComparisonOperatorEnum.Equal)
                    ));

            return sqlStatements;
        }
        private IList<ISqlStatement> GetTransformedRenameColumnStatement(RenameColumnStatement renameColumnStatement, string databaseIV)
        {
            var sqlStatements = new List<ISqlStatement>();

            var tableInfoRecord = _encryptor.FindInfoRecord(renameColumnStatement.TableName, databaseIV);


            var changeInfoRecordResults = _encryptor.ChangeInfoRecord(renameColumnStatement.ColumnName, renameColumnStatement.NewColumnName, tableInfoRecord.IV);
            var transformedOldColumnName = changeInfoRecordResults.Item1;
            var transformedNewColumnName = changeInfoRecordResults.Item2;

            sqlStatements.Add(
                new RenameColumnStatement(
                    new estring(tableInfoRecord.Name),
                    new estring(transformedOldColumnName),
                    new estring(transformedNewColumnName)));

            sqlStatements.Add(
               new UpdateRecordStatement(
                   INFO_TABLE_NAME,
                   new Dictionary<estring, Value>() { { NAME, new Value(transformedNewColumnName, true) } },
                   new ComparisonExpression(
                       INFO_TABLE_NAME,
                       NAME,
                       new Value(transformedOldColumnName, true),
                       ComparisonExpression.ComparisonOperatorEnum.Equal)
                   ));
            return sqlStatements;
        }
        private IList<ISqlStatement> GetTransformedAddColumnStatement(AddColumnStatement addColumnStatement, string databaseIV)
        {
            var sqlStatements = new List<ISqlStatement>();
            var tableInfoRecord = _encryptor.FindInfoRecord(addColumnStatement.TableName, databaseIV);
                
            var columnDefinitionsAndAdditionalInsert = GetTransformedColumnDefinition(addColumnStatement.ColumnDefinition, tableInfoRecord.IV, databaseIV);

            foreach (var columnDef in columnDefinitionsAndAdditionalInsert.Item1)
            {
                sqlStatements.Add(new AddColumnStatement(new estring(tableInfoRecord.Name), columnDef));
            }
            sqlStatements.AddRange(columnDefinitionsAndAdditionalInsert.Item2);
            return sqlStatements;
        }
        
        //TODO: NEEDS TO DROP ALSO BKT COLUMNS
        private IList<ISqlStatement> GetTransformedDropColumnStatement(DropColumnStatement dropColumnStatement, string databaseIV)
        {
            var sqlStatements = new List<ISqlStatement>();
            var tableInfoRecord = _encryptor.FindInfoRecord(dropColumnStatement.TableName, databaseIV);

            var columnToDelete = _encryptor.RemoveInfoRecord(dropColumnStatement.ColumnName, tableInfoRecord.IV);

            //foreach (var columnName in columnsToDelete)
            //{
            //    sqlStatements.Add( new DropColumnStatement(new estring(transformedTableName), new estring(columnName)));
            //    sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(columnName));
            //}

            return sqlStatements;
        }

        private ISqlStatement GetTransformedInsertRecordStatement(InsertRecordStatement insertRecordStatement, string databaseIV)
        {
            var tableInfoRecord = _encryptor.FindInfoRecord(insertRecordStatement.TableName, databaseIV);


            var transformedInsertRecordStatement = new InsertRecordStatement(new estring(tableInfoRecord.Name));
            var columnDataTypes = _encryptor.GetColumnDatatypes(tableInfoRecord.Name, databaseIV);

            foreach (var valuesPerColumn in insertRecordStatement.ValuesPerColumn)
            {
                transformedInsertRecordStatement.ValuesPerColumn.Concat(TransformColumnValues(valuesPerColumn, insertRecordStatement.TableName.Value, columnDataTypes));
            }

            return transformedInsertRecordStatement;
        }

        #endregion

        private IDictionary<estring, IList<Value>> TransformColumnValues(KeyValuePair<estring, IList<Value>> columnValues, string tableIV, Dictionary<string, string> columnDataTypes)
        {
            var columnName = columnValues.Key;

            var valuesPerColumn = new Dictionary<estring, IList<Value>>();

            var columnInfoRecord = _encryptor.FindInfoRecord(columnName, tableIV);
          
            if (columnInfoRecord == null) throw new FieldAccessException("No column with that name.");

            var bktColumnsNames = _encryptor.GetEncryptedBktColumnNames(columnInfoRecord.IV);

            string columnDataType = columnDataTypes[columnInfoRecord.Name];

            estring equalityBktColumnName = bktColumnsNames.Item1 != null ? new estring(bktColumnsNames.Item1) : null; 
            estring rangeBktColumnName = bktColumnsNames.Item2 != null ? new estring(bktColumnsNames.Item2) : null;
            estring ivColumnName = null;
                
            bool isEncrypted = columnDataType == ENCRYPTED_KEY_WORD;
            bool isNotUnique = equalityBktColumnName != null;

            if (isNotUnique)
            {
                valuesPerColumn[equalityBktColumnName] = new List<Value>();
                ivColumnName = _encryptor.GetIVColumnName(columnInfoRecord.Name);
                valuesPerColumn[ivColumnName] = new List<Value>();
            }
            if (rangeBktColumnName != null) valuesPerColumn[rangeBktColumnName] = new List<Value>();
           

            for (int i = 0; i < columnValues.Value.Count; i++)
            {
                columnValues.Value[i].ValueToInsert.Trim('\'', '"');

                if (isEncrypted)
                {
                    if (rangeBktColumnName != null)
                    {
                        var newRangeColumnValue = new Value(_encryptor.CreateRangeBktValue(rangeBktColumnName.Value, columnValues.Value[i].ValueToInsert, columnName.Value), true);
                        valuesPerColumn[rangeBktColumnName].Add(newRangeColumnValue);
                    }

                    if (isNotUnique)
                    {
                          var equalityBktValue = new Value(_encryptor.CreateEqualityBktValue(equalityBktColumnName.Value, columnValues.Value[i].ValueToInsert, columnName.Value), true);
                        valuesPerColumn[equalityBktColumnName].Add(equalityBktValue);

                        columnValues.Value[i] = new Value(_encryptor.EncryptNormalValue(columnValues.Value[i].ValueToInsert, columnName.Value, out string generatedIV), true);
                        valuesPerColumn[ivColumnName].Add(new Value(generatedIV, true));
                    }
                    else
                    {
                        columnValues.Value[i] = new Value(_encryptor.EncryptUniqueValue(columnValues.Value[i].ValueToInsert, columnName.Value), true);
                    }
                }

                else
                {
                    columnValues.Value[i] = new Value(columnValues.Value[i].ValueToInsert, columnDataType == TEXT_KEY_WORD);
                }
            }
            return valuesPerColumn;
        }

        private Tuple<IList<ColumnDefinition>, IList<InsertRecordStatement>> GetTransformedColumnDefinition(ColumnDefinition columnDefinition, string tableIV, string databaseIV)
        {
            var dataEncrypted = columnDefinition.DataType.DataTypeName == DataTypeEnum.ENCRYPTED;
            var columnInfoRecord = _encryptor.CreateInfoRecord(columnDefinition.ColumnName, tableIV); // data encrypted

            var insertRecordStatements = new List<InsertRecordStatement>() { CreateInsertRecordStatementForInfoTable(columnInfoRecord) };

            var transformedColumnDefinition = new ColumnDefinition(
                new estring(columnInfoRecord.Name),
                columnDefinition.DataType,
                columnDefinition.ColumnConstraints);

            var columnDefinitions = new List<ColumnDefinition>()
            {
                transformedColumnDefinition
            };

            if (columnDefinition.DataType.DataTypeName == DataTypeEnum.ENCRYPTED)
            {

                bool isUnique = columnDefinition.ColumnConstraints.Select(c => c.ColumnConstraintType).Contains(ColumnConstraintTypeEnum.PrimaryKey)
                    || columnDefinition.ColumnConstraints.Select(c => c.ColumnConstraintType).Contains(ColumnConstraintTypeEnum.Unique);

                if (!isUnique)
                {
                    columnDefinitions.Add(new ColumnDefinition(
                        _encryptor.GetIVColumnName(transformedColumnDefinition.ColumnName.Value),
                        new DataType() { DataTypeName = DataTypeEnum.TEXT }
                    ));
                    var createEqualityBktResult = CreateEqualityBucketColumnDefinition(columnDefinition, columnInfoRecord.Name);
                    columnDefinitions.Add(createEqualityBktResult.ColumnDefinition);
                    insertRecordStatements.Add(createEqualityBktResult.InsertRecordStatement);
                }

                if (columnDefinition.DataType.BucketInfo?.RangeBucketSize != null)
                {
                    var createRangeBktResult = CreateRangeBucketColumnDefinition(columnDefinition, columnInfoRecord.Name);
                    columnDefinitions.Add(createRangeBktResult.ColumnDefinition);
                    insertRecordStatements.Add(createRangeBktResult.InsertRecordStatement);
                }
            }

            foreach (var foreignKeyClause in columnDefinition.ColumnConstraints.Select(c => c.ForeignKeyClause).Where(f => f != null))
            {
                TransformForeignKeyClause(foreignKeyClause, databaseIV);
            }
            return new Tuple<IList<ColumnDefinition>, IList<InsertRecordStatement>>(columnDefinitions, insertRecordStatements);
        }

        private CreateBktColumnResultPoco CreateEqualityBucketColumnDefinition(ColumnDefinition columnDef, string columnName)
        {
            var equalityBktInfoRecord = _encryptor.CreateEqualityBktColumnName(columnName, columnDef.DataType.BucketInfo.EqualityBucketSize);

            return new CreateBktColumnResultPoco()
            {
                ColumnDefinition = new ColumnDefinition()
                {
                    ColumnName = new estring(equalityBktInfoRecord.Name),
                    DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                    ColumnConstraints = new List<ColumnConstraint>()
                },
                InsertRecordStatement = CreateInsertRecordStatementForInfoTable(equalityBktInfoRecord)
            };

        }
        private CreateBktColumnResultPoco CreateRangeBucketColumnDefinition(ColumnDefinition columnDef, string columnName)
        {
            var rangeBktInfoRecord = _encryptor.CreateRangeBktColumnName(columnName, columnDef.DataType.BucketInfo.RangeBucketSize,
                 columnDef.DataType.BucketInfo.BucketMinRange, columnDef.DataType.BucketInfo.BucketMaxRange);

            return new CreateBktColumnResultPoco()
            {
                ColumnDefinition = new ColumnDefinition()
                {
                    ColumnName = new estring(rangeBktInfoRecord.Name),
                    DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                    ColumnConstraints = new List<ColumnConstraint>()
                },
                InsertRecordStatement = CreateInsertRecordStatementForInfoTable(rangeBktInfoRecord)
            };
        }

        private ForeignKeyClause TransformForeignKeyClause(ForeignKeyClause foreignKeyClause, string databaseIV)
        {
            var tableInfoRecord = _encryptor.FindInfoRecord(foreignKeyClause.TableName, databaseIV);
            
            var transformedForeignKeyClause = new ForeignKeyClause(new estring(tableInfoRecord.Name));
            foreach (var columnName in foreignKeyClause.ColumnNames)
            {
                var columnInfoRecord = _encryptor.FindInfoRecord(columnName, tableInfoRecord.IV);
                transformedForeignKeyClause.ColumnNames.Add(new estring(columnInfoRecord.Name));
            }
            return transformedForeignKeyClause;
        }

        private CreateTableStatement CreateInfoTable()
        {
            return new CreateTableStatement()
            {
                TableName = INFO_TABLE_NAME,
                ColumnDefinitions = new List<ColumnDefinition>() {
                    new ColumnDefinition( NAME, new DataType(DataTypeEnum.TEXT), new List<ColumnConstraint>() { new ColumnConstraint { ColumnConstraintType = ColumnConstraintTypeEnum.NotNull } }),
                    new ColumnDefinition( IV, new DataType(DataTypeEnum.TEXT), new List<ColumnConstraint>() { new ColumnConstraint { ColumnConstraintType = ColumnConstraintTypeEnum.PrimaryKey } }),
                    new ColumnDefinition( PARENT, new DataType(DataTypeEnum.TEXT)),
                    new ColumnDefinition( KEY_READ, new DataType(DataTypeEnum.TEXT)),
                    new ColumnDefinition( KEY_MANAGE, new DataType(DataTypeEnum.TEXT), new List<ColumnConstraint>() { new ColumnConstraint { ColumnConstraintType = ColumnConstraintTypeEnum.NotNull } }),
                    new ColumnDefinition( DATA_ENCRYPTED, new DataType(DataTypeEnum.BOOL))
                   }
            };
        }
        private InsertRecordStatement CreateInsertRecordStatementForInfoTable(InfoRecord infoRecord)
        {
            return new InsertRecordStatement()
            {
                TableName = INFO_TABLE_NAME,
                ValuesPerColumn = new Dictionary<estring, IList<Value>>()
                {
                    { NAME, new List<Value>() { new Value(infoRecord.Name, true) }  },
                    { IV, new List<Value>() { new Value(infoRecord.IV, true) }  },
                    { PARENT, infoRecord.ParentIV != null ? new List<Value>() { new Value(infoRecord.ParentIV, true) } : null },
                    { KEY_READ, infoRecord.KeyName != null ? new List<Value>() { new Value(infoRecord.KeyName, true) } : null  },
                    { KEY_MANAGE, new List<Value>() { new Value(infoRecord.KeyManage, true) }  }
                }
            };
        }

        //adicionar netos
        private DeleteRecordStatement CreateDeleteRecordStatementForInfoTable(string iv)
        {
            return new DeleteRecordStatement(INFO_TABLE_NAME,
                    new LogicalExpression(
                        new ComparisonExpression(INFO_TABLE_NAME, NAME, new Value(iv, true), ComparisonExpression.ComparisonOperatorEnum.Equal),
                        new ComparisonExpression(INFO_TABLE_NAME, PARENT, new Value(iv, true), ComparisonExpression.ComparisonOperatorEnum.Equal),
                        LogicalExpression.LogicalOperatorEnum.OR
                        )
                    );
        }

    }
}
