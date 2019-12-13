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
        private static readonly estring DATA_ENCRYPTED = new estring(InfoTableConstants.DATA_ENCRYPTED);
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
            string currentDatabaseName = "";
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
                        transformedBuilder.AddStatement(GetTransformedUseDatabaseStatement(useDatabaseStatement, out currentDatabaseName));
                        break;

                    case CreateTableStatement createTableStatement:
                        transformedBuilder.AddStatements(GetTransformedCreateTableStatement(createTableStatement, currentDatabaseName));
                        break;

                    case DropTableStatement dropTableStatement:
                        transformedBuilder.AddStatements(GetTransformedDropTableStatement(dropTableStatement, currentDatabaseName));
                        break;

                    case AbstractAlterTableStatement abstractAlterTableStatement:
                        transformedBuilder.AddStatements(GetTransformedAlterTableStatement(abstractAlterTableStatement, currentDatabaseName));
                        break;
                }
            }
            return transformedBuilder;
        }

        private List<ISqlStatement> GetTransformedCreateDatabaseStatement(CreateDatabaseStatement createDatabaseStatement)
        {
            var datatabaseInfoRecord = _encryptor.CreateInfoRecord(createDatabaseStatement.DatabaseName);

            return new List<ISqlStatement>()
            {
                new CreateDatabaseStatement(new estring(datatabaseInfoRecord.Name)),
                CreateInfoTable(),
                CreateInsertRecordStatementForInfoTable(datatabaseInfoRecord)
            };
        }
        private List<ISqlStatement> GetTransformedDropDatabaseStatement(DropDatabaseStatement dropDatabaseStatement)
        {
            var databaseNameToRemove = _encryptor.RemoveInfoRecord(dropDatabaseStatement.DatabaseName);
            return new List<ISqlStatement>()
            {
                CreateDeleteRecordStatementForInfoTable(databaseNameToRemove),
                new DropDatabaseStatement(new estring(databaseNameToRemove))
            };
        }
        private UseDatabaseStatement GetTransformedUseDatabaseStatement(UseDatabaseStatement useDatabaseStatement, out string databaseName)
        {
            databaseName = _encryptor.GetEncryptedDatabaseName(useDatabaseStatement.DatabaseName);
            return new UseDatabaseStatement(new estring(databaseName));
        }

        private IList<ISqlStatement> GetTransformedCreateTableStatement(CreateTableStatement createTableStatement, string databaseName)
        {
            var tableInfoRecord = _encryptor.CreateInfoRecord(createTableStatement.TableName, databaseName);
            var transformedCreateTableStatement = new CreateTableStatement(new estring(tableInfoRecord.Name));

            var transformedStatements = new List<ISqlStatement>()
            {
                transformedCreateTableStatement,
                CreateInsertRecordStatementForInfoTable(tableInfoRecord)
            };

            foreach (var columnDef in createTableStatement.ColumnDefinitions)
            {
                var additionalColAndInfoInserts = GetTransformedColumnDefinition(columnDef, tableInfoRecord.Name, databaseName);

                ((List<ColumnDefinition>)createTableStatement.ColumnDefinitions).AddRange(additionalColAndInfoInserts.Item1);
                transformedStatements.Add(additionalColAndInfoInserts.Item2);
            }

            return transformedStatements;
        }
        private IList<ISqlStatement> GetTransformedDropTableStatement(DropTableStatement dropTableStatement, string databaseName)
        {
            var sqlStatements = new List<ISqlStatement>();
            var tableToDelete = _encryptor.RemoveInfoRecord(dropTableStatement.TableName, databaseName);
            sqlStatements.Add(new DropTableStatement(new estring(tableToDelete)));
            sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(tableToDelete));
            return sqlStatements;
        }

        private IList<ISqlStatement> GetTransformedAlterTableStatement(AbstractAlterTableStatement alterTableStatement, string databaseName)
        {
            switch (alterTableStatement)
            {
                case RenameTableStatement renameTableStatement:
                    return new List<ISqlStatement>(GetTransformedRenameTableStatement(renameTableStatement, databaseName));

                case RenameColumnStatement renameColumnStatement:
                    return new List<ISqlStatement>(GetTransformedRenameColumnStatement(renameColumnStatement, databaseName));

                case AddColumnStatement addColumnStatement:
                    return new List<ISqlStatement>(GetTransformedAddColumnStatement(addColumnStatement, databaseName));

                case DropColumnStatement dropColumnStatement:
                    return new List<ISqlStatement>(GetTransformedDropColumnStatement(dropColumnStatement, databaseName));
            }

            throw new FormatException("Alter table statement type not recognized.");
        }
        private IList<ISqlStatement> GetTransformedRenameTableStatement(RenameTableStatement renameTableStatement, string databaseName)
        {
            var sqlStatements = new List<ISqlStatement>();

            var changeInfoRecordResults = _encryptor.ChangeInfoRecord(renameTableStatement.TableName, renameTableStatement.NewTableName, databaseName);
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
        private IList<ISqlStatement> GetTransformedRenameColumnStatement(RenameColumnStatement renameColumnStatement, string databaseName)
        {
            var sqlStatements = new List<ISqlStatement>();

            var changeInfoRecordResults = _encryptor.ChangeInfoRecord(renameColumnStatement.ColumnName, renameColumnStatement.NewColumnName, databaseName);
            var transformedOldColumnName = changeInfoRecordResults.Item1;
            var transformedNewColumnName = changeInfoRecordResults.Item2;

            var transformedTableName  = _encryptor.GetEncryptedTableName(renameColumnStatement.TableName, databaseName);

            sqlStatements.Add(
                new RenameColumnStatement(
                    new estring(transformedTableName),
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
        private IList<ISqlStatement> GetTransformedAddColumnStatement(AddColumnStatement addColumnStatement, string databaseName)
        {
            var sqlStatements = new List<ISqlStatement>();
            var transformedTableName = _encryptor.GetEncryptedTableName(addColumnStatement.TableName, databaseName);
            var columnDefinitionsAndAdditionalInsert = GetTransformedColumnDefinition(addColumnStatement.ColumnDefinition, transformedTableName, databaseName);

            foreach (var columnDef in columnDefinitionsAndAdditionalInsert.Item1)
            {
                sqlStatements.Add(new AddColumnStatement(new estring(transformedTableName), columnDef));
            }
            sqlStatements.Add(columnDefinitionsAndAdditionalInsert.Item2);
            return sqlStatements;
        }
        private IList<ISqlStatement> GetTransformedDropColumnStatement(DropColumnStatement dropColumnStatement, string databaseName)
        {
            var sqlStatements = new List<ISqlStatement>();
            var transformedTableName = _encryptor.GetEncryptedTableName(dropColumnStatement.TableName, databaseName);
            var columnsToDelete = _encryptor.RemoveInfoRecord(dropColumnStatement.ColumnName, transformedTableName, databaseName);
            foreach (var columnName in columnsToDelete)
            {
                sqlStatements.Add( new DropColumnStatement(new estring(transformedTableName), new estring(columnName)));
                sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(columnName));
            }

            return sqlStatements;
        }
        private ISqlStatement GetTransformedInsertRecordStatement(InsertRecordStatement insertRecordStatement, string databaseName)
        {
            var transformedTableName = _encryptor.GetEncryptedTableName(insertRecordStatement.TableName, databaseName);
            var transformedInsertRecordStatement = new InsertRecordStatement(new estring(transformedTableName));
            var columnDataTypes = _encryptor.GetColumnDatatypes(transformedTableName, databaseName);

            foreach (var valuesPerColumn in insertRecordStatement.ValuesPerColumn)
            {
                transformedInsertRecordStatement.ValuesPerColumn.Concat(TransformColumnValues(valuesPerColumn, insertRecordStatement.TableName.Value, databaseName, columnDataTypes));
            }

            return transformedInsertRecordStatement;
        }

        private IDictionary<estring, IList<Value>> TransformColumnValues(KeyValuePair<estring, IList<Value>> columnValues, string tableName, string databaseName, Dictionary<string, string> columnDataTypes)
        {
            var columnName = columnValues.Key;

            var valuesPerColumn = new Dictionary<estring, IList<Value>>();

            var transformedColumnName = _encryptor.GetEncryptedColumnName(columnName, tableName, databaseName);
          
            if (transformedColumnName == null) throw new FieldAccessException("No column with that name.");

            var bktColumnsNames = _encryptor.GetEncryptedBktColumnNames(columnName, tableName, databaseName);

            string columnDataType = columnDataTypes[transformedColumnName];

            estring equalityBktColumnName = bktColumnsNames.Item1 != null ? new estring(bktColumnsNames.Item1) : null; 
            estring rangeBktColumnName = bktColumnsNames.Item2 != null ? new estring(bktColumnsNames.Item2) : null;
            estring ivColumnName = null;
                
            bool isEncrypted = columnDataType == ENCRYPTED_KEY_WORD;
            bool isNotUnique = equalityBktColumnName != null;

            if (equalityBktColumnName != null)
            {
                valuesPerColumn[equalityBktColumnName] = new List<Value>();
                ivColumnName = CreateIVColumnName(transformedColumnName);
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
            //            var newRangeColumnValue = new Value(CreateRangeBktValue(rangeBktColumnNameString, columnValues.Value[i].ValueToInsert, plainDatabaseName, plainTableName, columnName.Value), true);
            //            additionalColumnsPerColumn[rangeBktColumnName].Add(newRangeColumnValue);
                    }

            //        if (isNotUnique)
            //        {
            //            var equalityBktValue = new Value(CreateEqualityBktValue(equalityBktColumnNameString, columnValues.Value[i].ValueToInsert, plainDatabaseName, plainTableName, columnName.Value), true);
            //            additionalColumnsPerColumn[equalityBktColumnName].Add(equalityBktValue);

            //            columnValues.Value[i] = TransformNormalValue(columnValues.Value[i].ValueToInsert, columnName.Value, plainTableName, out byte[] generatedIV);
            //            additionalColumnsPerColumn[ivColumnName].Add(new Value(Base32Encoding.ZBase32.GetString(generatedIV), true));
            //        }

            //        else
            //        {
            //            columnValues.Value[i] = TransformUniqueValue(columnValues.Value[i].ValueToInsert, columnName.Value, plainTableName);
            //        }
                }

            //    else
            //    {
            //        columnValues.Value[i] = new Value(columnValues.Value[i].ValueToInsert, columnDataType == TEXT_KEY_WORD);
            //    }
            }
            return valuesPerColumn;
        }


        private Tuple<IList<ColumnDefinition>, InsertRecordStatement> GetTransformedColumnDefinition(ColumnDefinition columnDefinition, string tableName, string databaseName)
        {
            var dataEncrypted = columnDefinition.DataType.DataTypeName == DataTypeEnum.ENCRYPTED;
            var columnInfoRecord = _encryptor.CreateInfoRecord(columnDefinition.ColumnName, tableName, databaseName, dataEncrypted);

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
                        CreateIVColumnName(transformedColumnDefinition.ColumnName.Value),
                        new DataType() { DataTypeName = DataTypeEnum.TEXT }
                    ));

                    columnDefinitions.Add(CreateEqualityBucketColumnDefinition(columnDefinition, columnInfoRecord.Name));
                }

                if (columnDefinition.DataType.BucketInfo?.RangeBucketSize != null)
                    columnDefinitions.Add(CreateRangeBucketColumnDefinition(columnDefinition, columnInfoRecord.Name));
            }

            foreach (var foreignKeyClause in columnDefinition.ColumnConstraints.Select(c => c.ForeignKeyClause).Where(f => f != null))
            {
                TransformForeignKeyClause(foreignKeyClause, databaseName);
            }
            return new Tuple<IList<ColumnDefinition>, InsertRecordStatement>(columnDefinitions, CreateInsertRecordStatementForInfoTable(columnInfoRecord));
        }

        private estring CreateIVColumnName(string columnName)
        {
            return new estring(_ivPrefix + _separatingChar + columnName, false);
        }
        private estring CreateEqualityBktColumnName(string columnName, int? size)
        {
            var bucketColumnNameString = columnName.Substring(1, 4) + _separatingChar + size;
            var encryptedSizeAndRange = _encryptor.GetEncryptedBucketColumn(bucketColumnNameString);
            return new estring(_equalityBucketPrefix + _separatingChar + columnName + _separatingChar + encryptedSizeAndRange, false);
        }
        private estring CreateRangeBktColumnName(string columnName, int? size, int? min, int? max)
        {
            var bucketColumnNameString = columnName.Substring(1, 4) + _separatingChar + size + _separatingChar + min + _separatingChar + max;
            var encryptedSizeAndRange = _encryptor.GetEncryptedBucketColumn(bucketColumnNameString);

            return new estring(_rangeBucketPrefix + _separatingChar + columnName + _separatingChar + encryptedSizeAndRange, false);
        }

        private ColumnDefinition CreateEqualityBucketColumnDefinition(ColumnDefinition columnDef, string columnName)
        {
            var equalityBktColumnName = CreateEqualityBktColumnName(columnName, columnDef.DataType.BucketInfo.EqualityBucketSize);

            return new ColumnDefinition()
            {
                ColumnName = equalityBktColumnName,
                DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                ColumnConstraints = new List<ColumnConstraint>()
            };

        }
        private ColumnDefinition CreateRangeBucketColumnDefinition(ColumnDefinition columnDef, string columnName)
        {
            var rangeBktColumnName = CreateRangeBktColumnName(columnName, columnDef.DataType.BucketInfo.RangeBucketSize,
                 columnDef.DataType.BucketInfo.BucketMinRange, columnDef.DataType.BucketInfo.BucketMaxRange);

            return new ColumnDefinition()
            {
                ColumnName = rangeBktColumnName,
                DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                ColumnConstraints = new List<ColumnConstraint>()
            };
        }

        private string CreateRangeBktValue(string bktColumnName, string value, string databaseName, string tableName, string plainColumnName)
        {
            //var sizeAndRange = GetBktSizeAndRangeFromRangeBktColumnName(bktColumnName, databaseName, tableName);
            //if (double.TryParse(value, out double valueResult))
            //{
            //    var upperBoundValue = CalculateUpperBound(sizeAndRange.Item1, sizeAndRange.Item2, sizeAndRange.Item3, valueResult);
            //    return _separatingChar + _encryptor.GetRangeBucket(tableName, plainColumnName, upperBoundValue);
            //}
            //else throw new ArgumentException("Value was supposed to be a number.");
            return null;
        }

        private int CalculateUpperBound(int N, int min, int max, double value)
        {
            if (value < min || value > max) throw new ArgumentOutOfRangeException("The value you inserted is out of bounds.");

            for (int i = min + N - 1; i <= max; i += N)
            {
                if (value <= i)
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException("The value you inserted is out of bounds.");
        }

        private ForeignKeyClause TransformForeignKeyClause(ForeignKeyClause foreignKeyClause, string databaseName)
        {
            var encryptedTableName = _encryptor.GetEncryptedTableName(foreignKeyClause.TableName, databaseName);
            var transformedForeignKeyClause = new ForeignKeyClause(new estring(encryptedTableName));
            foreach (var columnName in foreignKeyClause.ColumnNames)
            {
                transformedForeignKeyClause.ColumnNames.Add(new estring(_encryptor.GetEncryptedColumnName(columnName, encryptedTableName, databaseName)));
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
                    { PARENT, infoRecord.Parent != null ? new List<Value>() { new Value(infoRecord.Parent, true) } : null },
                    { KEY_READ, infoRecord.KeyRead != null ? new List<Value>() { new Value(infoRecord.KeyRead, true) } : null  },
                    { KEY_MANAGE, new List<Value>() { new Value(infoRecord.KeyManage, true) }  }
                }
            };
        }
        private DeleteRecordStatement CreateDeleteRecordStatementForInfoTable(string name)
        {
            return new DeleteRecordStatement(INFO_TABLE_NAME,
                    new LogicalExpression(
                        new ComparisonExpression(INFO_TABLE_NAME, NAME, new Value(name, true), ComparisonExpression.ComparisonOperatorEnum.Equal),
                        new ComparisonExpression(INFO_TABLE_NAME, PARENT, new Value(name, true), ComparisonExpression.ComparisonOperatorEnum.Equal),
                        LogicalExpression.LogicalOperatorEnum.OR
                        )
                    );
        }

    }
}
