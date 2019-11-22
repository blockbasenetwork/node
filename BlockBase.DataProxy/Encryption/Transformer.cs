using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using Wiry.Base32;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.ColumnConstraint;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions.ComparisonExpression;

namespace BlockBase.DataProxy.Encryption
{
    public class Transformer
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

        private static readonly estring COLUMN_INFO_TABLE_NAME = new estring("column_info", false);
        private static readonly estring COLUMN_INFO_COLUMN_NAME = new estring("column_name", false);
        private static readonly estring COLUMN_INFO_NAME_ENCRYPTED = new estring("name_encrypted", false);
        private static readonly estring COLUMN_INFO_DATA_ENCRYPTED = new estring("data_encrypted", false);

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
                            var additionalStatement = Transform(createDatabaseStatement, entry.Key);
                            additionalStatements.Add(additionalStatement);
                            break;

                        case DropDatabaseStatement dropDatabaseStatement:
                            dropDatabaseStatement.DatabaseName = entry.Key;
                            break;

                        case UseDatabaseStatement useDatabaseStatement:
                            useDatabaseStatement.DatabaseName = entry.Key;
                            break;

                        case CreateTableStatement createTableStatement:
                            additionalStatements.AddRange(Transform(createTableStatement, entry.Key.Value));
                            break;

                        case AbstractAlterTableStatement abstractAlterTableStatement:
                            additionalStatements.AddRange(TransformAndGetAdditionalStatements(abstractAlterTableStatement, entry.Key));
                            break;

                        case DropTableStatement dropTableStatement:
                            additionalStatements.AddRange(Transform(dropTableStatement, entry.Key));
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

        private ISqlStatement Transform(CreateDatabaseStatement createDatabaseStatement, estring databaseName)
        {
            createDatabaseStatement.DatabaseName = databaseName;
            return new CreateTableStatement()
            {
                TableName = COLUMN_INFO_TABLE_NAME,
                ColumnDefinitions = new List<ColumnDefinition>() {
                    new ColumnDefinition( COLUMN_INFO_COLUMN_NAME, new DataType(DataTypeEnum.TEXT), new List<ColumnConstraint>() { new ColumnConstraint { ColumnConstraintType = ColumnConstraintTypeEnum.PrimaryKey } }),
                    new ColumnDefinition( COLUMN_INFO_NAME_ENCRYPTED, new DataType(DataTypeEnum.BOOL)),
                    new ColumnDefinition( COLUMN_INFO_DATA_ENCRYPTED, new DataType(DataTypeEnum.BOOL))
                }
            };
        }

        public IList<InsertRecordStatement> Transform(CreateTableStatement createTableStatement, string plainDatabaseName)
        {

            TransformTableName(createTableStatement.TableName, plainDatabaseName);

            var additionalColumns = new List<ColumnDefinition>();
            var infoInserts = new List<InsertRecordStatement>();

            for (int i = 0; i < createTableStatement.ColumnDefinitions.Count; i++)
            {
                var columnDef = createTableStatement.ColumnDefinitions[i];

                var additionalColAndInfoInserts = TransformColumnDefinitionAndGetAdditionalColumnsAndInsertInfo(columnDef, createTableStatement.TableName, plainDatabaseName);
                additionalColumns.AddRange(additionalColAndInfoInserts.Item1);
                infoInserts.Add(additionalColAndInfoInserts.Item2);
            }

            foreach (var additionalColumn in additionalColumns) createTableStatement.ColumnDefinitions.Add(additionalColumn);

            return infoInserts;
        }

        public IList<DeleteRecordStatement> Transform(DropTableStatement dropTableStatement, estring databaseName)
        {
            TransformTableName(dropTableStatement.TableName, databaseName.Value);
            var columns = GetTableColumnsWithoutBktAndIV(_psqlConnector.GetAllTableColumnsAndNameEncryptedIndicator(dropTableStatement.TableName.GetFinalString(), databaseName.GetFinalString()).Keys.ToList());
            var deleteInfos = new List<DeleteRecordStatement>();

            foreach (var column in columns)
            {
                deleteInfos.Add(
                    new DeleteRecordStatement()
                    {
                        TableName = COLUMN_INFO_TABLE_NAME,
                        WhereClause = new ComparisonExpression(
                            COLUMN_INFO_TABLE_NAME,
                            COLUMN_INFO_COLUMN_NAME,
                            "'" + column + "'",
                            ComparisonOperatorEnum.Equal)
                    }

                    );
            }
            return deleteInfos;

        }

        public IList<ISqlStatement> TransformAndGetAdditionalStatements(AbstractAlterTableStatement alterTableStatement, estring databaseName)
        {
            TransformTableName(alterTableStatement.TableName, databaseName.Value);

            //TODO refactor this, add column doesn't need to get info
            var allTableColumnsAndEncryptedNameIndicator = _psqlConnector.GetAllTableColumnsAndNameEncryptedIndicator(alterTableStatement.TableName.GetFinalString(), databaseName.EncryptedValue);

            switch (alterTableStatement)
            {
                case RenameTableStatement renameTableStatement:
                    return new List<ISqlStatement>(TransformAndGetAdditionalStatements(renameTableStatement, databaseName, allTableColumnsAndEncryptedNameIndicator));

                case RenameColumnStatement renameColumnStatement:
                    return new List<ISqlStatement>(TransformAndGetAdditionalStatements(renameColumnStatement, databaseName, allTableColumnsAndEncryptedNameIndicator.Keys.ToList()));

                case AddColumnStatement addColumnStatement:
                    return new List<ISqlStatement>(TransformAndGetAdditionalStatements(addColumnStatement, databaseName));

                case DropColumnStatement dropColumnStatement:
                    return new List<ISqlStatement>(TransformAndGetAdditionalStatements(dropColumnStatement, databaseName, allTableColumnsAndEncryptedNameIndicator.Keys.ToList()));
            }

            throw new FormatException("Alter table statement type not recognized.");
        }
        public IList<ISqlStatement> TransformAndGetAdditionalStatements(RenameTableStatement renameTableStatement, estring databaseName, IDictionary<string, bool> allTableColumnsAndEncryptedNameIndicator)
        {
            TransformTableName(renameTableStatement.NewTableName, databaseName.Value);

            var additionalColumns = new List<ISqlStatement>();

            foreach (var column in GetTableColumnsWithoutBktAndIV(allTableColumnsAndEncryptedNameIndicator.Keys.ToList()))
            {
                var isNameEncrypted = allTableColumnsAndEncryptedNameIndicator[column];
                
               
                    var renameColumnStatement = new RenameColumnStatement()
                    {
                        TableName = renameTableStatement.NewTableName,
                        ColumnName = new estring() { Value = isNameEncrypted ? null : column, EncryptedValue = isNameEncrypted ? column : null, ToEncrypt = false },
                        NewColumnName = new estring(isNameEncrypted ? _encryptor.DecryptColumnName(databaseName.Value, renameTableStatement.TableName.Value, column.Substring(1)) : column,
                                                     isNameEncrypted)
                    };
                
                additionalColumns.AddRange(TransformAndGetAdditionalStatements(renameColumnStatement, databaseName, allTableColumnsAndEncryptedNameIndicator.Keys.ToList(), renameTableStatement.TableName));
                if (isNameEncrypted) additionalColumns.Add(renameColumnStatement);
                
                
            }
            return additionalColumns;
        }
        public IList<ISqlStatement> TransformAndGetAdditionalStatements(RenameColumnStatement renameColumnStatement, estring databaseName, IList<string> allTableColumns, estring oldTableName = null)
        {
            var additionalStatements = new List<ISqlStatement>();

            if (oldTableName == null) TransformColumnName(renameColumnStatement.ColumnName, renameColumnStatement.TableName.Value, databaseName.Value);

            TransformColumnName(renameColumnStatement.NewColumnName, renameColumnStatement.TableName.Value, databaseName.Value);

            bool hasDifferentName = renameColumnStatement.ColumnName.GetFinalString() != renameColumnStatement.NewColumnName.GetFinalString();

            if (hasDifferentName) additionalStatements.Add(GetUpdateColumnInfo(renameColumnStatement.NewColumnName, renameColumnStatement.ColumnName));

            var equalityBktColumnName = GetColumnWithPrefix(renameColumnStatement.ColumnName.GetFinalString(), allTableColumns, _equalityBucketPrefix);
            var rangeBktColumnName = GetColumnWithPrefix(renameColumnStatement.ColumnName.GetFinalString(), allTableColumns, _rangeBucketPrefix);
            var ivColumnName = GetColumnWithPrefix(renameColumnStatement.ColumnName.GetFinalString(), allTableColumns, _ivPrefix);

            if (ivColumnName != null && hasDifferentName)
                additionalStatements.Add(new RenameColumnStatement(renameColumnStatement.TableName, new estring(ivColumnName), new estring(CreateIVColumnName(renameColumnStatement.NewColumnName.GetFinalString()))));

            if (equalityBktColumnName != null)
            {
                var bktEqualitySize = GetBktSizeFromEqualityBktColumnName(equalityBktColumnName, databaseName.Value, oldTableName != null ? oldTableName.Value : renameColumnStatement.TableName.Value);
                var newBktEqualityColumnName = CreateEqualityBktColumnName(renameColumnStatement.NewColumnName.GetFinalString(), bktEqualitySize, databaseName.Value, renameColumnStatement.TableName.Value);

                additionalStatements.Add(new RenameColumnStatement(renameColumnStatement.TableName, new estring(equalityBktColumnName), new estring(newBktEqualityColumnName)));
            }

            if (rangeBktColumnName != null)
            {
                var bktSizeRange = GetBktSizeAndRangeFromRangeBktColumnName(rangeBktColumnName, databaseName.Value, oldTableName != null ? oldTableName.Value : renameColumnStatement.TableName.Value);
                var newBktRangeColumnName = CreateRangeBktColumnName(renameColumnStatement.NewColumnName.GetFinalString(), bktSizeRange.Item1, bktSizeRange.Item2, bktSizeRange.Item3, databaseName.Value, renameColumnStatement.TableName.Value);

                additionalStatements.Add(new RenameColumnStatement(renameColumnStatement.TableName, new estring(rangeBktColumnName), new estring(newBktRangeColumnName)));
            }

            return additionalStatements;
        }
        public IList<ISqlStatement> TransformAndGetAdditionalStatements(AddColumnStatement addColumnStatement, estring databaseName)
        {
            var additionalColAndInfoInserts = TransformColumnDefinitionAndGetAdditionalColumnsAndInsertInfo(addColumnStatement.ColumnDefinition, addColumnStatement.TableName, databaseName.Value);
            var resultList = new List<ISqlStatement>(additionalColAndInfoInserts.Item1.Select(c =>

            new AddColumnStatement()
            {
                ColumnDefinition = c,
                TableName = addColumnStatement.TableName
            }).ToList());

            resultList.Add(additionalColAndInfoInserts.Item2);

            return resultList;
        }
        public IList<ISqlStatement> TransformAndGetAdditionalStatements(DropColumnStatement dropColumnStatement, estring databaseName, IList<string> allTableColumns)
        {
            TransformColumnName(dropColumnStatement.ColumnName, dropColumnStatement.TableName.Value, databaseName.Value);

            var additionalStatements = new List<ISqlStatement>();

            var equalityBktColumnName = GetColumnWithPrefix(dropColumnStatement.ColumnName.GetFinalString(), allTableColumns, _equalityBucketPrefix);
            var rangeBktColumnName = GetColumnWithPrefix(dropColumnStatement.ColumnName.GetFinalString(), allTableColumns, _rangeBucketPrefix);
            var ivColumnName = GetColumnWithPrefix(dropColumnStatement.ColumnName.GetFinalString(), allTableColumns, _ivPrefix);

            if (ivColumnName != null) additionalStatements.Add(new DropColumnStatement(dropColumnStatement.TableName, new estring(ivColumnName)));
            if (equalityBktColumnName != null) additionalStatements.Add(new DropColumnStatement(dropColumnStatement.TableName, new estring(equalityBktColumnName)));
            if (rangeBktColumnName != null) additionalStatements.Add(new DropColumnStatement(dropColumnStatement.TableName, new estring(rangeBktColumnName)));

            additionalStatements.Add( 
                new DeleteRecordStatement(
                COLUMN_INFO_TABLE_NAME, new ComparisonExpression(COLUMN_INFO_TABLE_NAME, COLUMN_INFO_COLUMN_NAME, "'" + dropColumnStatement.ColumnName.GetFinalString() + "'", ComparisonOperatorEnum.Equal)
                ));

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
        private IDictionary<estring, IList<string>> TransformColumnValues(KeyValuePair<estring, IList<string>> columnValues, string plainTableName, string plainDatabaseName, IDictionary<string, string> allTableColumnsAndDataTypes)
        {

            var columnName = columnValues.Key;

            var additionalColumnsPerColumn = new Dictionary<estring, IList<string>>();

            TransformColumnName(columnName, plainTableName, plainDatabaseName);

            if (!allTableColumnsAndDataTypes.Keys.Contains(columnName.GetFinalString())) throw new FieldAccessException("No column with that name.");

            var columnDataType = allTableColumnsAndDataTypes[columnName.GetFinalString()];

            var equalityBktColumnNameString = GetColumnWithPrefix(columnName.GetFinalString(), allTableColumnsAndDataTypes.Keys.ToList(), _equalityBucketPrefix);
            var equalityBktColumnName = equalityBktColumnNameString != null ? new estring(equalityBktColumnNameString) : null;

            var rangeBktColumnNameString = GetColumnWithPrefix(columnName.GetFinalString(), allTableColumnsAndDataTypes.Keys.ToList(), _rangeBucketPrefix);
            var rangeBktColumnName = rangeBktColumnNameString != null ? new estring(rangeBktColumnNameString) : null;

            var ivColumnNameString = GetColumnWithPrefix(columnName.GetFinalString(), allTableColumnsAndDataTypes.Keys.ToList(), _ivPrefix);
            var ivColumnName = ivColumnNameString != null ? new estring(ivColumnNameString) : null;

            bool isEncrypted = columnDataType == ENCRYPTED_KEY_WORD;
            bool isNotUnique = ivColumnName != null;

            if (equalityBktColumnName != null) additionalColumnsPerColumn[equalityBktColumnName] = new List<string>();
            if (rangeBktColumnName != null) additionalColumnsPerColumn[rangeBktColumnName] = new List<string>();
            if (isNotUnique) additionalColumnsPerColumn[ivColumnName] = new List<string>();

            for (int i = 0; i < columnValues.Value.Count; i++)
            {
                columnValues.Value[i] = columnValues.Value[i].Trim('\'', '"');

                if (isEncrypted)
                {
                    if (rangeBktColumnName != null)
                        additionalColumnsPerColumn[rangeBktColumnName].Add(CreateRangeBktValue(rangeBktColumnNameString, columnValues.Value[i], plainDatabaseName, plainTableName, columnName.Value));

                    if (isNotUnique)
                    {
                        additionalColumnsPerColumn[equalityBktColumnName].Add(CreateEqualityBktValue(equalityBktColumnNameString, columnValues.Value[i], plainDatabaseName, plainTableName, columnName.Value));
                        columnValues.Value[i] = TransformNormalValue(columnValues.Value[i], columnName.Value, plainTableName, out byte[] generatedIV);
                        additionalColumnsPerColumn[ivColumnName].Add("'" + Base32Encoding.ZBase32.GetString(generatedIV) + "'");
                    }

                    else
                    {
                        columnValues.Value[i] = TransformUniqueValue(columnValues.Value[i], columnName.Value, plainTableName);
                    }
                }

                if (columnDataType == TEXT_KEY_WORD || columnDataType == ENCRYPTED_KEY_WORD) columnValues.Value[i] = "'" + columnValues.Value[i] + "'";
            }

            return additionalColumnsPerColumn;

        }

        //TODO: Continue...
        public void Transform(UpdateRecordStatement updateRecordStatement, estring databaseName)
        {
            TransformTableName(updateRecordStatement.TableName, databaseName.Value);

            byte[] generatedIV = new byte[16];

            foreach (var entry in updateRecordStatement.ColumnNamesAndUpdateValues)
            {

                TransformColumnName(entry.Key, updateRecordStatement.TableName.Value, databaseName.Value);

                updateRecordStatement.ColumnNamesAndUpdateValues[entry.Key] = TransformNormalValue(entry.Value, entry.Key.Value, updateRecordStatement.TableName.Value, out generatedIV);
            }

            TransformExpression(updateRecordStatement.WhereClause, null, updateRecordStatement.TableName.Value, databaseName);
        }
        private void TransformExpression(AbstractExpression expression, string columnName, string plainTableName, estring databaseName)
        {
            switch (expression)
            {
                case ComparisonExpression comparisonExpression:
                    TransformComparisonExpression(comparisonExpression, databaseName);
                    break;

                case LogicalExpression logicalExpression:
                    TransformExpression(logicalExpression.LeftExpression, columnName, plainTableName, databaseName);
                    TransformExpression(logicalExpression.RightExpression, columnName, plainTableName, databaseName);
                    break;

            }
        }
        private void TransformComparisonExpression(ComparisonExpression comparisonExpression, estring databaseName)
        {
            TransformTableName(comparisonExpression.TableName, databaseName.Value);

            TransformColumnName(comparisonExpression.ColumnName, comparisonExpression.TableName.Value, databaseName.Value);

            var alltablecolumns = _psqlConnector.GetAllTableColumnsAndNameEncryptedIndicator(comparisonExpression.TableName.GetFinalString(), databaseName.GetFinalString());

            //var ivColumn = GetColumnWithPrefix(comparisonExpression.ColumnName.GetFinalString(), alltablecolumns, _ivPrefix);
            //var equalityBktColumn = GetColumnWithPrefix(comparisonExpression.ColumnName.GetFinalString(), alltablecolumns, _equalityBucketPrefix);
            //var rangeBktColumn = GetColumnWithPrefix(comparisonExpression.ColumnName.GetFinalString(), alltablecolumns, _rangeBucketPrefix);

            comparisonExpression.Value = TransformNormalValue(comparisonExpression.Value, comparisonExpression.ColumnName.Value, comparisonExpression.TableName.Value, out byte[] generatedIV);


        }

        private List<ComparisonExpression> TransformEqualityExpression(ComparisonExpression comparisonExpression, string ivColumn, string bktColumn, estring databaseName)
        {
            var additionalComparisonExpressions = new List<ComparisonExpression>();

            if (ivColumn != null)
            {


            }

            return additionalComparisonExpressions;
        }

        private Tuple<IList<ColumnDefinition>, InsertRecordStatement> TransformColumnDefinitionAndGetAdditionalColumnsAndInsertInfo(ColumnDefinition columnDefinition, estring tableName, string plainDatabaseName)
        {
            var additionalColumns = new List<ColumnDefinition>();
            TransformColumnName(columnDefinition.ColumnName, tableName.Value, plainDatabaseName);

            var infoInsert = new InsertRecordStatement(COLUMN_INFO_TABLE_NAME);
            infoInsert.ValuesPerColumn[COLUMN_INFO_COLUMN_NAME] = new List<string>() { "'" + columnDefinition.ColumnName.GetFinalString() + "'"};
            infoInsert.ValuesPerColumn[COLUMN_INFO_NAME_ENCRYPTED] = new List<string>() { ((columnDefinition.ColumnName.EncryptedValue != null) + "").ToUpper() };


            if (columnDefinition.DataType.DataTypeName == DataTypeEnum.ENCRYPTED)
            {
                infoInsert.ValuesPerColumn[COLUMN_INFO_DATA_ENCRYPTED] = new List<string>() { TRUE_KEY_WORD };
                bool isUnique = columnDefinition.ColumnConstraints.Select(c => c.ColumnConstraintType).Contains(ColumnConstraintTypeEnum.PrimaryKey)
                    || columnDefinition.ColumnConstraints.Select(c => c.ColumnConstraintType).Contains(ColumnConstraintTypeEnum.Unique);

                if (!isUnique)
                {
                    additionalColumns.Add(new ColumnDefinition()
                    {
                        ColumnName = new estring() { Value = CreateIVColumnName(columnDefinition.ColumnName.GetFinalString()), ToEncrypt = false },
                        DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                        ColumnConstraints = new List<ColumnConstraint>()
                    });
                }
                additionalColumns.AddRange(CreateBucketColumnDefinitions(columnDefinition, tableName.Value, plainDatabaseName, isUnique));
            }

            else infoInsert.ValuesPerColumn[COLUMN_INFO_DATA_ENCRYPTED] = new List<string>() { FALSE_KEY_WORD };

            foreach (var foreignKeyClause in columnDefinition.ColumnConstraints.Select(c => c.ForeignKeyClause).Where(f => f != null))
            {
                TransformForeignKeyClause(foreignKeyClause, plainDatabaseName);
            }
            return new Tuple<IList<ColumnDefinition>, InsertRecordStatement>(additionalColumns, infoInsert);
        }


        //TODO: REFACTOR THIS
        private void TransformColumnName(estring columnName, string tableName, string plainDatabaseName)
        {
            if (columnName.ToEncrypt)
            {
                columnName.EncryptedValue = _separatingChar + _encryptor.EncrypColumnName(plainDatabaseName, tableName, columnName.Value);
                columnName.ToEncrypt = false;
            }
        }
        private void TransformTableName(estring tableName, string plainDatabaseName)
        {
            if (tableName.ToEncrypt)
            {
                tableName.EncryptedValue = _separatingChar + _encryptor.EncrypTableName(plainDatabaseName, tableName.Value);
                tableName.ToEncrypt = false;
            }
        }
        private void TransformDatabaseName(estring databaseName)
        {
            if (databaseName.ToEncrypt)
            {
                databaseName.EncryptedValue = _separatingChar + _encryptor.EncryptDatabaseName(databaseName.Value);
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

        private string CreateEqualityBktValue(string bktColumnName, string value, string plainDatabaseName, string plainTableName, string plainColumnName)
        {
            int bktSize = GetBktSizeFromEqualityBktColumnName(bktColumnName, plainDatabaseName, plainTableName);
            return "'" + _encryptor.GetEqualityBucket(plainTableName, plainColumnName, value, bktSize) + "'";
        }

        private string CreateRangeBktValue(string bktColumnName, string value, string plainDatabaseName, string plainTableName, string plainColumnName)
        {
            var sizeAndRange = GetBktSizeAndRangeFromRangeBktColumnName(bktColumnName, plainDatabaseName, plainTableName);
            if (double.TryParse(value, out double valueResult))
            {
                var upperBoundValue = CalculateUpperBound(sizeAndRange.Item1, sizeAndRange.Item2, sizeAndRange.Item3, valueResult);
                return "'" + _separatingChar + _encryptor.GetRangeBucket(plainTableName, plainColumnName, upperBoundValue + "") + "'";
            }
            else throw new ArgumentException("Value was supposed to be a number.");
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


        private IList<ColumnDefinition> CreateBucketColumnDefinitions(ColumnDefinition columnDef, string plainTableName, string plainDatabaseName, bool isUnique)
        {
            var bktColumnDefs = new List<ColumnDefinition>();

            if (columnDef.DataType.BucketInfo.EqualityBucketSize != null)
            {
                var equalityBktColumnName = CreateEqualityBktColumnName(columnDef.ColumnName.GetFinalString(), columnDef.DataType.BucketInfo.EqualityBucketSize,
                     plainDatabaseName, plainTableName);

                bktColumnDefs.Add(new ColumnDefinition()
                {
                    ColumnName = new estring(equalityBktColumnName),
                    DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                    ColumnConstraints = new List<ColumnConstraint>()
                });
            }

            if (columnDef.DataType.BucketInfo.RangeBucketSize != null)
            {
                var rangeBktColumnName = CreateRangeBktColumnName(columnDef.ColumnName.GetFinalString(), columnDef.DataType.BucketInfo.RangeBucketSize,
                     columnDef.DataType.BucketInfo.BucketMinRange, columnDef.DataType.BucketInfo.BucketMaxRange, plainDatabaseName, plainTableName);

                bktColumnDefs.Add(new ColumnDefinition()
                {
                    ColumnName = new estring(rangeBktColumnName),
                    DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                    ColumnConstraints = new List<ColumnConstraint>()
                });
            }

            return bktColumnDefs;
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
            return _ivPrefix + _separatingChar + columnName;
        }
        private string CreateEqualityBktColumnName(string columnName, int? size, string plainDatabaseName, string plainTableName)
        {
            var bucketColumnNameString = columnName.Substring(1, 4) + _separatingChar + size;
            var encryptedSizeAndRange = _encryptor.EncrypColumnName(plainDatabaseName, plainTableName, bucketColumnNameString);
            return _equalityBucketPrefix + _separatingChar + columnName + _separatingChar + encryptedSizeAndRange;
        }
        private string CreateRangeBktColumnName(string columnName, int? size, int? min, int? max, string plainDatabaseName, string plainTableName)
        {
            var bucketColumnNameString = columnName.Substring(1, 4) + _separatingChar + size + _separatingChar + min + _separatingChar + max;
            var encryptedSizeAndRange = _encryptor.EncrypColumnName(plainDatabaseName, plainTableName, bucketColumnNameString);

            return _rangeBucketPrefix + _separatingChar + columnName + _separatingChar + encryptedSizeAndRange;
        }


        private int GetBktSizeFromEqualityBktColumnName(string bktColumnName, string databaseName, string tableName)
        {
            var encryptedBktSizeAndRange = bktColumnName.Split(new string[] { _separatingChar + _separatingChar, _separatingChar }, StringSplitOptions.None)[2];
            var bktSize = _encryptor.DecryptColumnName(databaseName, tableName, encryptedBktSizeAndRange).Split(_separatingChar);

            return Int32.Parse(bktSize[1]);
        }
        private Tuple<int, int, int> GetBktSizeAndRangeFromRangeBktColumnName(string bktColumnName, string databaseName, string tableName)
        {
            var encryptedBktSizeAndRange = bktColumnName.Split(new string[] { _separatingChar + _separatingChar, _separatingChar }, StringSplitOptions.None)[2];
            var bktSizeAndRange = _encryptor.DecryptColumnName(databaseName, tableName, encryptedBktSizeAndRange).Split(_separatingChar);

            return new Tuple<int, int, int>(Int32.Parse(bktSizeAndRange[1]), Int32.Parse(bktSizeAndRange[2]), Int32.Parse(bktSizeAndRange[3]));
        }

        public string GetColumnWithPrefix(string columnName, IList<string> allTableColumns, string prefix)
        {
            return allTableColumns.Where(c => c.StartsWith(prefix + _separatingChar + columnName)).SingleOrDefault();
        }
        public IList<string> GetTableColumnsWithoutBktAndIV(IList<string> allTableColumns)
        {
            return allTableColumns.Where(c => !c.StartsWith(_ivPrefix) && !c.StartsWith(_bucketPrefix)).ToList();
        }

        public UpdateRecordStatement GetUpdateColumnInfo(estring newColumnName, estring columnName)
        {
            return new UpdateRecordStatement()
            {
                TableName = COLUMN_INFO_TABLE_NAME,
                ColumnNamesAndUpdateValues = new Dictionary<estring, string>() {
                    { COLUMN_INFO_COLUMN_NAME, "'" + newColumnName.GetFinalString() + "'"},
                    { COLUMN_INFO_NAME_ENCRYPTED, ((newColumnName.EncryptedValue != null) + "").ToUpper()  }
                },
                WhereClause = new ComparisonExpression(COLUMN_INFO_TABLE_NAME, COLUMN_INFO_COLUMN_NAME, "'" + columnName.GetFinalString() + "'", ComparisonOperatorEnum.Equal)
            };
        }

    }
}
