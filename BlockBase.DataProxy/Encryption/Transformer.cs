using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using System;
using System.Collections.Generic;
using System.Text;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using System.Linq;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.ColumnConstraint;
using Wiry.Base32;
using BlockBase.DataPersistence.Sidechain.Connectors;

namespace BlockBase.DataProxy.Encryption
{
    public class Transformer
    {
        //TODO: refactor this to go to configs
        private static string _encryptionAuxiliarChar = "_";
        private static string _ivPrefix = "iv";
        private static string _bucketPrefix = "bkt";

        private PSqlConnector _psqlConnector;
        private Encryptor _encryptor;

        //TODO: this will not have a psql conector, instead it will have a classe that will communicate with the producer
        public Transformer(PSqlConnector psqlConnector)
        {
            _psqlConnector = psqlConnector;
            _encryptor = new Encryptor();
        }
        public Builder Transform(Builder builder)
        {
            foreach (var entry in builder.SqlStatementsPerDatabase)
            {
                TransformDatabaseName(entry.Key);
                var additionalStatements = new List<ISqlStatement>();

                for (int i = 0; i < entry.Value.Count; i++)
                {
                    switch (entry.Value[i])
                    {
                        case CreateDatabaseStatement createDatabaseStatement:
                            createDatabaseStatement.DatabaseName = entry.Key;
                            break;

                        case DropDatabaseStatement dropDatabaseStatement:
                            dropDatabaseStatement.DatabaseName = entry.Key;
                            break;

                        case UseDatabaseStatement useDatabaseStatement:
                            useDatabaseStatement.DatabaseName = entry.Key;
                            break;

                        case CreateTableStatement createTableStatement:
                            Transform(createTableStatement, entry.Key.Value);
                            break;

                        case AbstractAlterTableStatement abstractAlterTableStatement:
                            additionalStatements.AddRange(TransformAndGetAdditionalStatements(abstractAlterTableStatement, entry.Key));
                            break;

                        case DropTableStatement dropTableStatement:
                            Transform(dropTableStatement, entry.Key.Value);
                            break;

                        case InsertRecordStatement insertRecordStatement:
                            Transform(insertRecordStatement, entry.Key);
                            break;

                            //case UpdateRecordStatement updateRecordStatement:
                            //    Transform(updateRecordStatement, entry.Key.Value);
                            //    break;

                            //case DeleteRecordStatement deleteRecordStatement:
                            //    Transform(deleteRecordStatement, entry.Key.Value);
                            //break;

                            //case SimpleSelectStatement simpleSelectStatement:
                            //    _transformer.Transform(simpleSelectStatement);
                            //    break;

                            //case SelectCoreStatement selectCoreStatement:
                            //    _transformer.Transform(selectCoreStatement);
                            //    break;


                    }

                }

                foreach (var additionalAlterTableStatement in additionalStatements)
                    builder.AddStatement(additionalAlterTableStatement, entry.Key);

            }
            return builder;
        }

        public void Transform(CreateTableStatement createTableStatement, string plainDatabaseName)
        {

            TransformTableName(createTableStatement.TableName, plainDatabaseName);

            var additionalColumns = new List<ColumnDefinition>();

            for (int i = 0; i < createTableStatement.ColumnDefinitions.Count; i++)
            {
                var columnDef = createTableStatement.ColumnDefinitions[i];

                additionalColumns.AddRange(TransformColumnDefinitionAndGetAdditionalColumns(columnDef, createTableStatement.TableName.Value, plainDatabaseName));
            }

            foreach (var additionalColumn in additionalColumns) createTableStatement.ColumnDefinitions.Add(additionalColumn);
        }

        public void Transform(DropTableStatement dropTableStatement, string plainDatabaseName)
        {
            TransformTableName(dropTableStatement.TableName, plainDatabaseName);
        }

        public IList<AbstractAlterTableStatement> TransformAndGetAdditionalStatements(AbstractAlterTableStatement alterTableStatement, estring databaseName)
        {
            TransformTableName(alterTableStatement.TableName, databaseName.Value);

            //TODO refactor this, add column doesn't need to get info
            var allTableColumns = _psqlConnector.GetAllTableColumns(alterTableStatement.TableName.GetFinalString(), databaseName.EncryptedValue);

            switch (alterTableStatement)
            {
                case RenameTableStatement renameTableStatement:
                    return new List<AbstractAlterTableStatement>(TransformAndGetAdditionalStatements(renameTableStatement, databaseName, allTableColumns));

                case RenameColumnStatement renameColumnStatement:
                    return new List<AbstractAlterTableStatement>(TransformAndGetAdditionalStatements(renameColumnStatement, databaseName, allTableColumns));

                case AddColumnStatement addColumnStatement:
                    return new List<AbstractAlterTableStatement>(TransformAndGetAdditionalStatements(addColumnStatement, databaseName));

                case DropColumnStatement dropColumnStatement:
                    return new List<AbstractAlterTableStatement>(TransformAndGetAdditionalStatements(dropColumnStatement, databaseName, allTableColumns));
            }

            throw new FormatException("Alter table statement type not recognized.");
        }
        public IList<RenameColumnStatement> TransformAndGetAdditionalStatements(RenameTableStatement renameTableStatement, estring databaseName, IList<string> allTableColumns)
        {
            TransformTableName(renameTableStatement.NewTableName, databaseName.Value);

            var additionalColumns = new List<RenameColumnStatement>();

            foreach (var column in GetTableColumnsWithoutBkt(allTableColumns))
            {
                //Console.WriteLine(_encryptor.DecryptColumnName(plainDatabaseName, plainTableName, column.Substring(1)));
                var renameColumnStatement = new RenameColumnStatement()
                {
                    TableName = renameTableStatement.NewTableName,
                    ColumnName = new estring() { Value = column.StartsWith(_encryptionAuxiliarChar) ? null : column, EncryptedValue = column.StartsWith(_encryptionAuxiliarChar) ? column : null, ToEncrypt = false },
                    NewColumnName = new estring(column.StartsWith(_encryptionAuxiliarChar) ? _encryptor.DecryptColumnName(databaseName.Value, renameTableStatement.TableName.Value, column.Substring(1)) : column,
                                                 column.StartsWith(_encryptionAuxiliarChar))

                };

                additionalColumns.AddRange(TransformAndGetAdditionalStatements(renameColumnStatement, databaseName, allTableColumns, renameTableStatement.TableName));
                additionalColumns.Add(renameColumnStatement);
            }
            return additionalColumns;
        }
        public IList<RenameColumnStatement> TransformAndGetAdditionalStatements(RenameColumnStatement renameColumnStatement, estring databaseName, IList<string> allTableColumns, estring oldTableName = null)
        {
            var additionalStatements = new List<RenameColumnStatement>();

            if (oldTableName == null) TransformColumnName(renameColumnStatement.ColumnName, renameColumnStatement.TableName.Value, databaseName.Value);

            TransformColumnName(renameColumnStatement.NewColumnName, renameColumnStatement.TableName.Value, databaseName.Value);

            var bktColumnName = GetColumnCorrespondentBKTColumnName(renameColumnStatement.ColumnName.GetFinalString(), allTableColumns);

            var ivColumnName = GetColumnCorrespondentIVColumnName(renameColumnStatement.ColumnName.GetFinalString(), allTableColumns);

            if (ivColumnName != null)
            {
                additionalStatements.Add(new RenameColumnStatement()
                {
                    TableName = renameColumnStatement.TableName,
                    ColumnName = new estring()
                    {
                        ToEncrypt = false,
                        Value = ivColumnName
                    },
                    NewColumnName = new estring()
                    {
                        ToEncrypt = false,
                        Value = CreateIVColumnName(renameColumnStatement.NewColumnName.GetFinalString())
                    }
                });
            }
            if (bktColumnName != null)
            {
                var bktSizeAndRange = GetBktSizeAndRangeFromBktColumnName(bktColumnName, databaseName.Value, oldTableName != null ? oldTableName.Value : renameColumnStatement.TableName.Value);

                var newBktColumnName = bktSizeAndRange.Item2 == null ?
                    CreateBKTColumnName(renameColumnStatement.NewColumnName.GetFinalString(), bktSizeAndRange.Item1, null, null, databaseName.Value, renameColumnStatement.TableName.Value)
                    : CreateBKTColumnName(renameColumnStatement.NewColumnName.GetFinalString(), bktSizeAndRange.Item1, bktSizeAndRange.Item2.Item1, bktSizeAndRange.Item2.Item2, databaseName.Value, renameColumnStatement.TableName.GetFinalString());

                additionalStatements.Add(new RenameColumnStatement()
                {
                    TableName = renameColumnStatement.TableName,
                    ColumnName = new estring()
                    {
                        ToEncrypt = false,
                        Value = bktColumnName
                    },
                    NewColumnName = new estring()
                    {
                        ToEncrypt = false,
                        Value = newBktColumnName
                    }
                });

            }

            return additionalStatements;
        }
        public IList<AddColumnStatement> TransformAndGetAdditionalStatements(AddColumnStatement addColumnStatement, estring databaseName)
        {
            return TransformColumnDefinitionAndGetAdditionalColumns(addColumnStatement.ColumnDefinition, addColumnStatement.TableName.Value, databaseName.Value).Select(c =>

            new AddColumnStatement()
            {
                ColumnDefinition = c,
                TableName = addColumnStatement.TableName
            }).ToList();
        }
        public IList<DropColumnStatement> TransformAndGetAdditionalStatements(DropColumnStatement dropColumnStatement, estring databaseName, IList<string> allTableColumns)
        {
            TransformColumnName(dropColumnStatement.ColumnName, dropColumnStatement.TableName.Value, databaseName.Value);

            var additionalStatements = new List<DropColumnStatement>();

            var bktColumnName = GetColumnCorrespondentBKTColumnName(dropColumnStatement.ColumnName.GetFinalString(), allTableColumns);
            var ivColumnName = GetColumnCorrespondentIVColumnName(dropColumnStatement.ColumnName.GetFinalString(), allTableColumns);

            if (ivColumnName != null)
            {
                additionalStatements.Add(new DropColumnStatement()
                {
                    TableName = dropColumnStatement.TableName,
                    ColumnName = new estring()
                    {
                        ToEncrypt = false,
                        Value = ivColumnName
                    }
                });
            }
            if (bktColumnName != null)
            {
                additionalStatements.Add(new DropColumnStatement()
                {
                    TableName = dropColumnStatement.TableName,
                    ColumnName = new estring()
                    {
                        ToEncrypt = false,
                        Value = bktColumnName
                    }
                });
            }

            return additionalStatements;
        }

        public void Transform(InsertRecordStatement insertRecordStatement, estring databaseName)
        {
            TransformTableName(insertRecordStatement.TableName, databaseName.Value);

            var totalAdditionalColumnsAndValues = new Dictionary<estring, IList<string>>();

            var allTableColumnsAndDataTypes = _psqlConnector.GetAllTableColumnsAndDataTypes(insertRecordStatement.TableName.GetFinalString(), databaseName.GetFinalString());

            foreach (var valuesPerColumn in insertRecordStatement.ValuesPerColumn)
            {
                var additionalColumnsAndValues = TransformColumnValues(valuesPerColumn, insertRecordStatement.TableName.Value, databaseName.Value, allTableColumnsAndDataTypes);

                foreach (var valuesPerAdditionalColumn in additionalColumnsAndValues)
                {
                    totalAdditionalColumnsAndValues[valuesPerAdditionalColumn.Key] = valuesPerAdditionalColumn.Value;
                }
            }

            foreach (var valuesPerAdditionalColumn in totalAdditionalColumnsAndValues)
            {
                insertRecordStatement.ValuesPerColumn[valuesPerAdditionalColumn.Key] = valuesPerAdditionalColumn.Value;
            }

        }
        private IDictionary<estring, IList<string>> TransformColumnValues(KeyValuePair<estring, IList<string>> columnValues, string tableName, string plainDatabaseName, IDictionary<string, string> allTableColumnsAndDataTypes)
        {

            var columnName = columnValues.Key;

            var additionalColumnsPerColumn = new Dictionary<estring, IList<string>>();

            TransformColumnName(columnName, tableName, plainDatabaseName);

            if (!allTableColumnsAndDataTypes.Keys.Contains(columnName.GetFinalString())) throw new FieldAccessException("No column with that name.");

            var columnDataType = allTableColumnsAndDataTypes[columnName.GetFinalString()];

            var bktColumnNameString = GetColumnCorrespondentBKTColumnName(columnName.GetFinalString(), allTableColumnsAndDataTypes.Keys.ToList());
            var bktColumnName = bktColumnNameString != null ? new estring(bktColumnNameString) : null;

            var ivColumnNameString = GetColumnCorrespondentIVColumnName(columnName.GetFinalString(), allTableColumnsAndDataTypes.Keys.ToList());
            var ivColumnName = ivColumnNameString != null ? new estring(ivColumnNameString) : null;

            bool isEncrypted = bktColumnName != null;
            bool isNotUnique = ivColumnName != null;

            if (isEncrypted) additionalColumnsPerColumn[bktColumnName] = new List<string>();
            if (isNotUnique) additionalColumnsPerColumn[ivColumnName] = new List<string>();

            for (int i = 0; i < columnValues.Value.Count; i++)
            {
                columnValues.Value[i] = columnValues.Value[i].Trim('\'', '"');

                if (isEncrypted)
                {
                    if (isNotUnique)
                    {
                        byte[] generatedIV = new byte[16];
                        columnValues.Value[i] = TransformNormalValue(columnValues.Value[i], columnName.Value, tableName, out generatedIV);
                        additionalColumnsPerColumn[ivColumnName].Add("'" + Base32Encoding.ZBase32.GetString(generatedIV) + "'");
                    }

                    else {
                        columnValues.Value[i] = TransformUniqueValue(columnValues.Value[i], columnName.Value, tableName);
                    }
                    additionalColumnsPerColumn[bktColumnName].Add("'xxxxx'");
                    //CalculateUpperBound(int N, int min, int max, int value)
                }

                if (columnDataType == "text")  columnValues.Value[i] = "'" + columnValues.Value[i] + "'";
            }

            return additionalColumnsPerColumn;

        }

        public void Transform(UpdateRecordStatement updateRecordStatement, string plainDatabaseName)
        {
            TransformTableName(updateRecordStatement.TableName, plainDatabaseName);

            byte[] generatedIV = new byte[16];

            foreach (var entry in updateRecordStatement.ColumnNamesAndUpdateValues)
            {
            
                TransformColumnName(entry.Key, updateRecordStatement.TableName.Value, plainDatabaseName);

                updateRecordStatement.ColumnNamesAndUpdateValues[entry.Key] = TransformNormalValue(entry.Value, entry.Key.Value, updateRecordStatement.TableName.Value, out generatedIV);

            }

            TransformExpression(updateRecordStatement.WhereClause, null, updateRecordStatement.TableName.Value, plainDatabaseName);
        }

       


        private void TransformExpression(AbstractExpression expression, string columnName, string tableName, string plainDatabaseName)
        {
            switch (expression)
            {
                case ComparisonExpression comparisonExpression:
                    TransformTableName(comparisonExpression.TableName, plainDatabaseName);

                    TransformColumnName(comparisonExpression.ColumnName, comparisonExpression.TableName.Value, plainDatabaseName);

                    comparisonExpression.Value = TransformNormalValue(comparisonExpression.Value, comparisonExpression.ColumnName.Value, comparisonExpression.TableName.Value, out byte[] generatedIV);
                    break;

                case LogicalExpression logicalExpression:
                    TransformExpression(logicalExpression.LeftExpression, columnName, tableName, plainDatabaseName);
                    TransformExpression(logicalExpression.RightExpression, columnName, tableName, plainDatabaseName);
                    break;

            }
        }

        private IList<ColumnDefinition> TransformColumnDefinitionAndGetAdditionalColumns(ColumnDefinition columnDefinition, string plainTableName, string plainDatabaseName)
        {
            var additionalColumns = new List<ColumnDefinition>();

            var plainColumnName = columnDefinition.ColumnName.Value;

            TransformColumnName(columnDefinition.ColumnName, plainTableName, plainDatabaseName);

            if (columnDefinition.DataType.DataTypeName == DataTypeEnum.ENCRYPTED)
            {
                if (!columnDefinition.ColumnConstraints.Select(c => c.ColumnConstraintType).Contains(ColumnConstraintTypeEnum.PrimaryKey)
                    && !columnDefinition.ColumnConstraints.Select(c => c.ColumnConstraintType).Contains(ColumnConstraintTypeEnum.Unique))
                {
                    additionalColumns.Add(new ColumnDefinition()
                    {
                        ColumnName = new estring() { Value = CreateIVColumnName(columnDefinition.ColumnName.GetFinalString()), ToEncrypt = false },
                        DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                        ColumnConstraints = new List<ColumnConstraint>()
                    });
                }

                if (columnDefinition.DataType.BucketSize != null)
                    additionalColumns.Add(CreateBucketColumnDefinition(columnDefinition, plainTableName, plainDatabaseName));
            }

            foreach (var foreignKeyClause in columnDefinition.ColumnConstraints.Select(c => c.ForeignKeyClause).Where(f => f != null))
            {
                TransformForeignKeyClause(foreignKeyClause, plainDatabaseName);
            }

            return additionalColumns;
        }


        //TODO: REFACTOR THIS
        private void TransformColumnName(estring columnName, string tableName, string plainDatabaseName)
        {
            if (columnName.ToEncrypt)
            {
                columnName.EncryptedValue = _encryptionAuxiliarChar + _encryptor.EncrypColumnName(plainDatabaseName, tableName, columnName.Value);
                columnName.ToEncrypt = false;
            }
        }
        private void TransformTableName(estring tableName, string plainDatabaseName)
        {
            if (tableName.ToEncrypt)
            {
                tableName.EncryptedValue = _encryptionAuxiliarChar + _encryptor.EncrypTableName(plainDatabaseName, tableName.Value);
                tableName.ToEncrypt = false;
            }
        }
        private void TransformDatabaseName(estring databaseName)
        {
            if (databaseName.ToEncrypt)
            {
                databaseName.EncryptedValue = _encryptionAuxiliarChar + _encryptor.EncryptDatabaseName(databaseName.Value);
                databaseName.ToEncrypt = false;
            }
        }


        private string TransformNormalValue(string value, string columnName, string tableName, out byte[] generatedIV)
        {
            return _encryptor.EncryptNormalValue(value, columnName, tableName, out generatedIV);
        }

        private string TransformUniqueValue(string value, string columnName, string tableName)
        {
            return _encryptor.EncryptUniqueValue(tableName, columnName, value);
        }

        private ColumnDefinition CreateBucketColumnDefinition(ColumnDefinition columnDef, string plainTableName, string plainDatabaseName)
        {
            var bucketColumnName = CreateBKTColumnName(columnDef.ColumnName.GetFinalString(), columnDef.DataType.BucketSize,
                columnDef.DataType.BucketMinRange, columnDef.DataType.BucketMaxRange, plainDatabaseName, plainTableName);

            return new ColumnDefinition()
            {
                ColumnName = new estring(bucketColumnName),
                DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                ColumnConstraints = new List<ColumnConstraint>()
            };
        }

        private void TransformForeignKeyClause(ForeignKeyClause foreignKeyClause, string plainDatabaseName)
        {
            var plainTableName = foreignKeyClause.ForeignTableName.Value;
            TransformTableName(foreignKeyClause.ForeignTableName, plainDatabaseName);
            foreach (var columnName in foreignKeyClause.ColumnNames)
            {
                TransformColumnName(columnName, plainTableName, plainDatabaseName);
            }
        }

        private string CreateIVColumnName(string columnName)
        {
            return _ivPrefix + columnName;
        }
        private string CreateBKTColumnName(string columnName, int? size, int? min, int? max, string plainDatabaseName, string plainTableName)
        {
            var bucketColumnNameString = columnName.Substring(1, 4) + _encryptionAuxiliarChar + size;

            if (min != null && max != null)
                bucketColumnNameString += _encryptionAuxiliarChar + min + _encryptionAuxiliarChar + max;

            var encryptedSizeAndRange = _encryptor.EncrypColumnName(plainDatabaseName, plainTableName, bucketColumnNameString);
            Console.WriteLine("Bkt final string: " + _bucketPrefix + columnName + _encryptionAuxiliarChar + encryptedSizeAndRange);
            return _bucketPrefix + columnName + _encryptionAuxiliarChar + encryptedSizeAndRange;

        }


        private Tuple<int, Tuple<int, int>> GetBktSizeAndRangeFromBktColumnName(string bktColumnName, string databaseName, string tableName)
        {
            var encryptedBktSizeAndRange = bktColumnName.Split(_encryptionAuxiliarChar)[2];
            var bktSizeAndRangeString = _encryptor.DecryptColumnName(databaseName, tableName, encryptedBktSizeAndRange).Split(_encryptionAuxiliarChar);

            if (bktSizeAndRangeString.Count() != 4) return new Tuple<int, Tuple<int, int>>(Int32.Parse(bktSizeAndRangeString[1]), null);

            return new Tuple<int, Tuple<int, int>>(Int32.Parse(bktSizeAndRangeString[1]), new Tuple<int, int>(Int32.Parse(bktSizeAndRangeString[2]), Int32.Parse(bktSizeAndRangeString[3])));
        }

        public string GetColumnCorrespondentBKTColumnName(string columnName, IList<string> allTableColumns)
        {
            return allTableColumns.Where(c => c.StartsWith(_bucketPrefix + columnName)).SingleOrDefault();
        }

        public string GetColumnCorrespondentIVColumnName(string columnName, IList<string> allTableColumns)
        {
            return allTableColumns.Where(c => c == _ivPrefix + columnName).SingleOrDefault();
        }

        public IList<string> GetTableColumnsWithoutBkt(IList<string> allTableColumns)
        {
            return allTableColumns.ToList();
        }


        private int CalculateUpperBound(int N, int min, int max, int value)
        {
            if (value < min || value > max) throw new ArgumentOutOfRangeException("The value you inserted is out of bounds.");

            for (int i = min + N; i <= max; i += N)
            {
                if (value <= i)
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException("The value you inserted is out of bounds.");
        }
    }
}
