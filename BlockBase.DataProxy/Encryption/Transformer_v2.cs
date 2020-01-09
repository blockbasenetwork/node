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
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions.ComparisonExpression;
using BlockBase.Domain.Database.Sql.SqlCommand;

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
        private static readonly estring DATA = new estring(InfoTableConstants.DATA);
        private static readonly estring KEY_NAME = new estring(InfoTableConstants.KEY_NAME);
        private static readonly estring KEY_MANAGE = new estring(InfoTableConstants.KEY_MANAGE);
        private static readonly estring PARENT = new estring(InfoTableConstants.PARENT);
        private static readonly estring IV = new estring(InfoTableConstants.IV);
        private InfoRecord _databaseInfoRecord = null;

        private PSqlConnector _psqlConnector;
        private IEncryptor _encryptor;

        //TODO: this will not have a psql conector, instead it will have a classe that will communicate with the producer
        public Transformer_v2(PSqlConnector psqlConnector, MiddleMan middleMan)
        {
            _psqlConnector = psqlConnector;
            _encryptor = middleMan;
        }

        public void TransformBuilder(Builder builder)
        {
            foreach (var command in builder.SqlCommands)
            {
                var statement = command.OriginalSqlStatement;
                switch (statement)
                {
                    case CreateDatabaseStatement createDatabaseStatement:
                        command.TransformedSqlStatement = GetTransformedCreateDatabaseStatement(createDatabaseStatement, out _databaseInfoRecord);
                        var createDatabaseCommand = (DatabaseSqlCommand)command;
                        createDatabaseCommand.DatabaseName = ((CreateDatabaseStatement)command.TransformedSqlStatement[0]).DatabaseName.Value;
                        break;

                    case DropDatabaseStatement dropDatabaseStatement:
                        command.TransformedSqlStatement = GetTransformedDropDatabaseStatement(dropDatabaseStatement);
                        break;

                    case UseDatabaseStatement useDatabaseStatement:
                        command.TransformedSqlStatement = new List<ISqlStatement>() { GetTransformedUseDatabaseStatement(useDatabaseStatement, out _databaseInfoRecord) };
                        var useDatabaseCommand = (DatabaseSqlCommand)command;
                        useDatabaseCommand.DatabaseName = ((CreateDatabaseStatement)command.TransformedSqlStatement[0]).DatabaseName.Value;
                        break;

                    case CreateTableStatement createTableStatement:
                        command.TransformedSqlStatement = GetTransformedCreateTableStatement(createTableStatement, _databaseInfoRecord.IV);
                        break;

                    case DropTableStatement dropTableStatement:
                        command.TransformedSqlStatement = GetTransformedDropTableStatement(dropTableStatement, _databaseInfoRecord.IV);
                        break;

                    case AbstractAlterTableStatement abstractAlterTableStatement:
                        command.TransformedSqlStatement = GetTransformedAlterTableStatement(abstractAlterTableStatement, _databaseInfoRecord.IV);
                        break;

                    case InsertRecordStatement insertRecordStatement:
                        command.TransformedSqlStatement = new List<ISqlStatement>() { GetTransformedInsertRecordStatement(insertRecordStatement, _databaseInfoRecord.IV) };
                        break;

                    case SimpleSelectStatement simpleSelectStatement:
                        command.TransformedSqlStatement = new List<ISqlStatement>() { GetTransformedSimpleSelectStatement(simpleSelectStatement, _databaseInfoRecord.IV) };
                        break;
                }
            }
        }
        #region Transform SqlStatements
        private List<ISqlStatement> GetTransformedCreateDatabaseStatement(CreateDatabaseStatement createDatabaseStatement, out InfoRecord databaseInfoRecord)
        {
            databaseInfoRecord = _encryptor.CreateInfoRecord(createDatabaseStatement.DatabaseName, null);

            return new List<ISqlStatement>()
            {
                new CreateDatabaseStatement(new estring(databaseInfoRecord.Name)),
                CreateInfoTable(),
                CreateInsertRecordStatementForInfoTable(databaseInfoRecord)
            };
        }
        private List<ISqlStatement> GetTransformedDropDatabaseStatement(DropDatabaseStatement dropDatabaseStatement)
        {
            var infoRecord = _encryptor.FindInfoRecord(dropDatabaseStatement.DatabaseName, null);

            var sqlStatements = new List<ISqlStatement>()
            {
                new DropDatabaseStatement(new estring(infoRecord.Name))
            };

            var childrenInfoRecords = _encryptor.FindChildren(infoRecord.IV, true);
            _encryptor.RemoveInfoRecord(infoRecord);

            foreach (var child in childrenInfoRecords) sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(child.IV));

            return sqlStatements;
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

                ((List<ColumnDefinition>)transformedCreateTableStatement.ColumnDefinitions).AddRange(additionalColAndInfoInserts.Item1);
                transformedStatements.Add(additionalColAndInfoInserts.Item2);
            }

            return transformedStatements;
        }
        private IList<ISqlStatement> GetTransformedDropTableStatement(DropTableStatement dropTableStatement, string databaseIV)
        {
            var infoRecord = _encryptor.FindInfoRecord(dropTableStatement.TableName, databaseIV);

            var sqlStatements = new List<ISqlStatement>()
            {
                new DropTableStatement(new estring(infoRecord.Name))
            };

            var childrenInfoRecords = _encryptor.FindChildren(infoRecord.IV, true);
            sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(infoRecord.IV));

            foreach (var child in childrenInfoRecords)
            {
                sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(child.IV));
            }

            _encryptor.RemoveInfoRecord(infoRecord);

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

            var tableInfoRecord = _encryptor.FindInfoRecord(renameTableStatement.TableName, databaseIV);
            var transformedOldTableName = tableInfoRecord.Name;

            tableInfoRecord = _encryptor.ChangeInfoRecordName(tableInfoRecord, renameTableStatement.NewTableName);
            var transformedNewTableName = tableInfoRecord.Name;

            sqlStatements.Add(new RenameTableStatement(new estring(transformedOldTableName), new estring(transformedNewTableName)));

            sqlStatements.Add(
                new UpdateRecordStatement(
                    INFO_TABLE_NAME,
                    new Dictionary<estring, Value>() {
                        { NAME, new Value(transformedNewTableName, true) },
                        { KEY_NAME, tableInfoRecord.KeyName != null ? new Value(tableInfoRecord.KeyName, true) : new Value("null", false) }
                    },
                    new ComparisonExpression(
                        INFO_TABLE_NAME,
                        IV,
                        new Value(tableInfoRecord.IV, true),
                        ComparisonExpression.ComparisonOperatorEnum.Equal)
                    ));

            return sqlStatements;
        }
        private IList<ISqlStatement> GetTransformedRenameColumnStatement(RenameColumnStatement renameColumnStatement, string databaseIV)
        {
            var sqlStatements = new List<ISqlStatement>();

            var tableInfoRecord = _encryptor.FindInfoRecord(renameColumnStatement.TableName, databaseIV);

            var columnInfoRecord = _encryptor.FindInfoRecord(renameColumnStatement.ColumnName, tableInfoRecord.IV);
            var transformedOldColumnName = columnInfoRecord.Name;

            columnInfoRecord = _encryptor.ChangeInfoRecordName(columnInfoRecord, renameColumnStatement.NewColumnName);
            var transformedNewColumnName = columnInfoRecord.Name;

            sqlStatements.Add(
                new RenameColumnStatement(
                    new estring(tableInfoRecord.Name),
                    new estring(transformedOldColumnName),
                    new estring(transformedNewColumnName)));

            sqlStatements.Add(
               new UpdateRecordStatement(
                   INFO_TABLE_NAME,
                   new Dictionary<estring, Value>() { { NAME, new Value(transformedNewColumnName, true) },
                   { KEY_NAME, tableInfoRecord.KeyName != null ? new Value(columnInfoRecord.KeyName, true) : new Value("null", false) }},
                   new ComparisonExpression(
                       INFO_TABLE_NAME,
                       IV,
                       new Value(columnInfoRecord.IV, true),
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
            sqlStatements.Add(columnDefinitionsAndAdditionalInsert.Item2);
            return sqlStatements;
        }

        private IList<ISqlStatement> GetTransformedDropColumnStatement(DropColumnStatement dropColumnStatement, string databaseIV)
        {
            var tableInfoRecord = _encryptor.FindInfoRecord(dropColumnStatement.TableName, databaseIV);

            var columnInfoRecord = _encryptor.FindInfoRecord(dropColumnStatement.ColumnName, tableInfoRecord.IV);

            var sqlStatements = new List<ISqlStatement>()
            {
                new DropColumnStatement(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.Name))
            };

            if (columnInfoRecord.LData.EncryptedEqualityColumnName != null)
                sqlStatements.Add(new DropColumnStatement(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedEqualityColumnName)));

            if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                sqlStatements.Add(new DropColumnStatement(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedIVColumnName)));

            if (columnInfoRecord.LData.EncryptedRangeColumnName != null)
                sqlStatements.Add(new DropColumnStatement(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedRangeColumnName)));

            sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(columnInfoRecord.IV));
            _encryptor.RemoveInfoRecord(columnInfoRecord);



            return sqlStatements;
        }

        private ISqlStatement GetTransformedInsertRecordStatement(InsertRecordStatement insertRecordStatement, string databaseIV)
        {
            var tableInfoRecord = _encryptor.FindInfoRecord(insertRecordStatement.TableName, databaseIV);


            var transformedInsertRecordStatement = new InsertRecordStatement(new estring(tableInfoRecord.Name));

            foreach (var valuesPerColumn in insertRecordStatement.ValuesPerColumn)
            {
                var transformedValuesPerColumn = TransformColumnValues(valuesPerColumn, tableInfoRecord.IV);
                foreach (var keyPair in transformedValuesPerColumn) transformedInsertRecordStatement.ValuesPerColumn.Add(keyPair);
            }

            return transformedInsertRecordStatement;
        }

        private ISqlStatement GetTransformedSimpleSelectStatement(SimpleSelectStatement simpleSelectStatement, string databaseIV)
        {
            var transformedSimpleSelectStatement = new SimpleSelectStatement();
            transformedSimpleSelectStatement.SelectCoreStatement = (SelectCoreStatement)GetTransformedSelectCoreStatement(simpleSelectStatement.SelectCoreStatement, databaseIV);
            return transformedSimpleSelectStatement;
        }

        private ISqlStatement GetTransformedSelectCoreStatement(SelectCoreStatement selectCoreStatement, string databaseIV)
        {
            var transformedSelectStatement = new SelectCoreStatement();

            foreach (var resultColumn in selectCoreStatement.ResultColumns)
            {
                var tableInfoRecord = _encryptor.FindInfoRecord(resultColumn.TableName, databaseIV);
                var columnInfoRecord = !resultColumn.AllColumnsfFlag ? _encryptor.FindInfoRecord(resultColumn.ColumnName, tableInfoRecord.IV) : null;

                transformedSelectStatement.ResultColumns.Add(new ResultColumn()
                {
                    TableName = new estring(tableInfoRecord.Name),
                    ColumnName = !resultColumn.AllColumnsfFlag ? new estring(columnInfoRecord.Name) : null,
                    AllColumnsfFlag = resultColumn.AllColumnsfFlag
                });

                if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                {

                    transformedSelectStatement.ResultColumns.Add(new ResultColumn()
                    {
                        TableName = new estring(tableInfoRecord.Name),
                        ColumnName = new estring(columnInfoRecord.LData.EncryptedIVColumnName),
                        AllColumnsfFlag = resultColumn.AllColumnsfFlag
                    });
                }
            }

            transformedSelectStatement.TablesOrSubqueries = GetTransformedTablesOrSubqueries(selectCoreStatement.TablesOrSubqueries, databaseIV);

            transformedSelectStatement.WhereExpression = GetTransformedExpression(selectCoreStatement.WhereExpression, databaseIV, transformedSelectStatement);

            return transformedSelectStatement;
        }

        //TODO: finish this method
        private IList<TableOrSubquery> GetTransformedTablesOrSubqueries(IList<TableOrSubquery> tablesOrSubqueries, string databaseIV)
        {
            var transformedTableOrSubqueries = new List<TableOrSubquery>();

            foreach (var tableOrSubquery in tablesOrSubqueries)
            {
                if (tableOrSubquery.TableName != null)
                {
                    transformedTableOrSubqueries.Add(new TableOrSubquery()
                    {
                        TableName = new estring(_encryptor.FindInfoRecord(tableOrSubquery.TableName, databaseIV).Name)
                    });
                }
            }

            return transformedTableOrSubqueries;

        }

        private AbstractExpression GetTransformedExpression(AbstractExpression expression, string databaseIV, SelectCoreStatement transformedSelectCoreStatement)
        {
            switch (expression)
            {
                case ComparisonExpression comparisonExpression:
                    return GetTransformedComparisonExpression(comparisonExpression, databaseIV, transformedSelectCoreStatement);

                case LogicalExpression logicalExpression:
                    var newLogicalExpression = (LogicalExpression)logicalExpression.Clone();
                    newLogicalExpression.LeftExpression = GetTransformedExpression(logicalExpression.LeftExpression, databaseIV, transformedSelectCoreStatement);
                    newLogicalExpression.RightExpression = GetTransformedExpression(logicalExpression.RightExpression, databaseIV, transformedSelectCoreStatement);
                    return newLogicalExpression;

            }
            throw new FormatException("Expression not recognized.");
        }

        private AbstractExpression GetTransformedComparisonExpression(ComparisonExpression comparisonExpression, string databaseIV, SelectCoreStatement transformedSelectCoreStatement)
        {
            var tableInfoRecord = _encryptor.FindInfoRecord(comparisonExpression.TableName, databaseIV);

            var columnInfoRecord = _encryptor.FindInfoRecord(comparisonExpression.ColumnName, tableInfoRecord.IV);
            var columnDataType = _encryptor.GetColumnDataType(columnInfoRecord);

            var transformedComparisonExpression = new ComparisonExpression(
                new estring(tableInfoRecord.Name),
                new estring(columnInfoRecord.Name),
                comparisonExpression.Value,
                comparisonExpression.ComparisonOperator);

            transformedSelectCoreStatement.TryAddResultColumn(new TableAndColumnName(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.Name)));

            if (columnDataType.DataTypeName == DataTypeEnum.ENCRYPTED)
            {
                var isColumnUnique = columnInfoRecord.LData.EncryptedIVColumnName == null;
                if (!isColumnUnique)  transformedSelectCoreStatement.TryAddResultColumn(new TableAndColumnName(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedIVColumnName)));

                if (comparisonExpression.ComparisonOperator == ComparisonOperatorEnum.Equal || comparisonExpression.ComparisonOperator == ComparisonOperatorEnum.Different)
                {
                    if (!isColumnUnique)
                    {
                        transformedComparisonExpression.ColumnName = new estring(columnInfoRecord.LData.EncryptedEqualityColumnName);
                        transformedComparisonExpression.Value = new Value(_encryptor.CreateEqualityBktValue(comparisonExpression.Value.ValueToInsert, columnInfoRecord, columnDataType), true);
                    }
                }
                else
                {
                    transformedComparisonExpression.ColumnName = new estring(columnInfoRecord.LData.EncryptedRangeColumnName);
                    if (double.TryParse(comparisonExpression.Value.ValueToInsert, out double valueDoubleToInsert))
                        transformedComparisonExpression.Value = new Value(_encryptor.CreateRangeBktValue(valueDoubleToInsert, columnInfoRecord, columnDataType), true);
                    else
                        throw new Exception("Tried to compare variable that is not a number.");
                }
            }
            return transformedComparisonExpression;
        }


        #endregion

        private IDictionary<estring, IList<Value>> TransformColumnValues(KeyValuePair<estring, IList<Value>> columnValues, string tableIV)
        {
            var columnName = columnValues.Key;

            var valuesPerColumn = new Dictionary<estring, IList<Value>>();

            var columnInfoRecord = _encryptor.FindInfoRecord(columnName, tableIV);

            if (columnInfoRecord == null) throw new FieldAccessException("No column with that name.");

            var columnDataType = _encryptor.GetColumnDataType(columnInfoRecord);

            estring equalityBktColumnName = columnInfoRecord.LData.EncryptedEqualityColumnName != null ? new estring(columnInfoRecord.LData.EncryptedEqualityColumnName) : null;
            estring rangeBktColumnName = columnInfoRecord.LData.EncryptedRangeColumnName != null ? new estring(columnInfoRecord.LData.EncryptedRangeColumnName) : null;
            estring ivColumnName = columnInfoRecord.LData.EncryptedIVColumnName != null ? new estring(columnInfoRecord.LData.EncryptedIVColumnName) : null;

            bool isEncrypted = columnDataType.DataTypeName == DataTypeEnum.ENCRYPTED;
            bool isNotUnique = ivColumnName != null;

            valuesPerColumn[new estring(columnInfoRecord.Name)] = new List<Value>();
            if (isEncrypted && isNotUnique)
            {
                valuesPerColumn[equalityBktColumnName] = new List<Value>();
                valuesPerColumn[ivColumnName] = new List<Value>();
            }
            if (rangeBktColumnName != null) valuesPerColumn[rangeBktColumnName] = new List<Value>();


            for (int i = 0; i < columnValues.Value.Count; i++)
            {

                if (isEncrypted)
                {
                    if (rangeBktColumnName != null)
                    {
                        bool tryParse = double.TryParse(columnValues.Value[i].ValueToInsert, out double doubleValue);
                        if (!tryParse) throw new FormatException("The value in a range column needs to be a number.");
                        var newRangeColumnValue = new Value(_encryptor.CreateRangeBktValue(doubleValue, columnInfoRecord, columnDataType), true);
                        valuesPerColumn[rangeBktColumnName].Add(newRangeColumnValue);
                    }

                    if (isNotUnique)
                    {
                        var equalityBktValue = new Value(_encryptor.CreateEqualityBktValue(columnValues.Value[i].ValueToInsert, columnInfoRecord, columnDataType), true);
                        valuesPerColumn[equalityBktColumnName].Add(equalityBktValue);

                        valuesPerColumn[new estring(columnInfoRecord.Name)].Add(new Value(_encryptor.EncryptNormalValue(columnValues.Value[i].ValueToInsert, columnInfoRecord, out string generatedIV), true));
                        valuesPerColumn[ivColumnName].Add(new Value(generatedIV, true));
                    }
                    else
                    {
                        valuesPerColumn[new estring(columnInfoRecord.Name)].Add(new Value(_encryptor.EncryptUniqueValue(columnValues.Value[i].ValueToInsert, columnInfoRecord), true));
                    }
                }
                else
                {
                    valuesPerColumn[new estring(columnInfoRecord.Name)].Add(new Value(columnValues.Value[i].ValueToInsert, columnDataType.ToString() == TEXT_KEY_WORD));
                }
            }
            return valuesPerColumn;
        }

        private Tuple<IList<ColumnDefinition>, InsertRecordStatement> GetTransformedColumnDefinition(ColumnDefinition columnDefinition, string tableIV, string databaseIV)
        {
            var columnInfoRecord = _encryptor.CreateColumnInfoRecord(columnDefinition.ColumnName, tableIV, columnDefinition.DataType);


            var transformedColumnDefinition = new ColumnDefinition(
                new estring(columnInfoRecord.Name),
                columnDefinition.DataType,
                new List<ColumnConstraint>());


            foreach (var columnConstraint in columnDefinition.ColumnConstraints)
            {
                transformedColumnDefinition.ColumnConstraints.Add(
                    new ColumnConstraint()
                    {
                        //TODO: encrypt column constraint name
                        Name = columnConstraint.Name,
                        ColumnConstraintType = columnConstraint.ColumnConstraintType,
                        ForeignKeyClause = columnConstraint.ForeignKeyClause != null ?
                        TransformForeignKeyClause(columnConstraint.ForeignKeyClause, databaseIV) : null
                    })
                    ;
            }

            var columnDefinitions = new List<ColumnDefinition>()
            {
                transformedColumnDefinition
            };

            if (columnInfoRecord.LData.EncryptedEqualityColumnName != null)
                columnDefinitions.Add(new ColumnDefinition(new estring(columnInfoRecord.LData.EncryptedEqualityColumnName), new DataType(DataTypeEnum.TEXT)));

            if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                columnDefinitions.Add(new ColumnDefinition(new estring(columnInfoRecord.LData.EncryptedIVColumnName), new DataType(DataTypeEnum.TEXT)));

            if (columnInfoRecord.LData.EncryptedRangeColumnName != null)
                columnDefinitions.Add(new ColumnDefinition(new estring(columnInfoRecord.LData.EncryptedRangeColumnName), new DataType(DataTypeEnum.TEXT)));

            return new Tuple<IList<ColumnDefinition>, InsertRecordStatement>(columnDefinitions, CreateInsertRecordStatementForInfoTable(columnInfoRecord));
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
                    new ColumnDefinition( PARENT, new DataType(DataTypeEnum.TEXT) ),
                    new ColumnDefinition( KEY_NAME, new DataType(DataTypeEnum.TEXT) ),
                    new ColumnDefinition( KEY_MANAGE, new DataType(DataTypeEnum.TEXT), new List<ColumnConstraint>() { new ColumnConstraint { ColumnConstraintType = ColumnConstraintTypeEnum.NotNull } }),
                    new ColumnDefinition( DATA, new DataType(DataTypeEnum.TEXT) )
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
                    { PARENT, infoRecord.ParentIV != null ? new List<Value>() { new Value(infoRecord.ParentIV, true) } : new List<Value>() { new Value("null", false) } },
                    { KEY_NAME, infoRecord.KeyName != null ? new List<Value>() { new Value(infoRecord.KeyName, true) } : new List<Value>() { new Value("null", false) }  },
                    { KEY_MANAGE, new List<Value>() { new Value(infoRecord.KeyManage, true) }  },
                    { DATA, infoRecord.Data != null ? new List<Value>() { new Value(infoRecord.Data, true) } : new List<Value>() { new Value("null", false) }  },
                }
            };
        }

        private DeleteRecordStatement CreateDeleteRecordStatementForInfoTable(string iv)
        {
            return new DeleteRecordStatement(INFO_TABLE_NAME,
                new ComparisonExpression(INFO_TABLE_NAME, NAME, new Value(iv, true), ComparisonExpression.ComparisonOperatorEnum.Equal)
                    );
        }
    }
}
