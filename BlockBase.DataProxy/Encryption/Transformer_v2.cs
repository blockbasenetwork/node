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
            var infoRecord = _encryptor.FindInfoRecord(dropDatabaseStatement.DatabaseName, null);

            var sqlStatements = new List<ISqlStatement>()
            {
                new DropDatabaseStatement(new estring(infoRecord.Name))
            };

            var childrenInfoRecords = _encryptor.FindChildren(infoRecord.IV, true);
            _encryptor.RemoveInfoRecord(infoRecord.IV);

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

                ((List<ColumnDefinition>)createTableStatement.ColumnDefinitions).AddRange(additionalColAndInfoInserts.Item1);
                transformedStatements.Add(additionalColAndInfoInserts.Item2);
            }

            return transformedStatements;
        }
        private IList<ISqlStatement> GetTransformedDropTableStatement(DropTableStatement dropTableStatement, string databaseName)
        {
            var infoRecord = _encryptor.FindInfoRecord(dropTableStatement.TableName, databaseName);

            var sqlStatements = new List<ISqlStatement>()
            {
                new DropTableStatement(new estring(infoRecord.Name))
            };

            var childrenInfoRecords = _encryptor.FindChildren(infoRecord.IV, true);
            _encryptor.RemoveInfoRecord(infoRecord.IV);

            foreach (var child in childrenInfoRecords) sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(child.IV));
            
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
            sqlStatements.Add(columnDefinitionsAndAdditionalInsert.Item2);
            return sqlStatements;
        }
        
        
        private IList<ISqlStatement> GetTransformedDropColumnStatement(DropColumnStatement dropColumnStatement, string databaseIV)
        {
            var tableInfoRecord = _encryptor.FindInfoRecord(dropColumnStatement.TableName, databaseIV);

            var infoRecord = _encryptor.FindInfoRecord(dropColumnStatement.ColumnName, tableInfoRecord.IV);

            var sqlStatements = new List<ISqlStatement>()
            {
                new DropTableStatement(new estring(infoRecord.Name))
            };

            var childrenInfoRecords = _encryptor.FindChildren(infoRecord.IV, true);
            sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(infoRecord.IV));
            _encryptor.RemoveInfoRecord(infoRecord.IV);

            foreach (var child in childrenInfoRecords) sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(child.IV));

            return sqlStatements;
        }

        private ISqlStatement GetTransformedInsertRecordStatement(InsertRecordStatement insertRecordStatement, string databaseIV)
        {
            var tableInfoRecord = _encryptor.FindInfoRecord(insertRecordStatement.TableName, databaseIV);


            var transformedInsertRecordStatement = new InsertRecordStatement(new estring(tableInfoRecord.Name));

            foreach (var valuesPerColumn in insertRecordStatement.ValuesPerColumn)
            {
                transformedInsertRecordStatement.ValuesPerColumn.Concat(TransformColumnValues(valuesPerColumn, insertRecordStatement.TableName.Value));
            }

            return transformedInsertRecordStatement;
        }

        #endregion

        private IDictionary<estring, IList<Value>> TransformColumnValues(KeyValuePair<estring, IList<Value>> columnValues, string tableIV)
        {
            var columnName = columnValues.Key;

            var valuesPerColumn = new Dictionary<estring, IList<Value>>();

            var columnInfoRecord = _encryptor.FindInfoRecord(columnName, tableIV);
          
            if (columnInfoRecord == null) throw new FieldAccessException("No column with that name.");

            var columnDataType = _encryptor.GetColumnDatatype(columnInfoRecord.IV);

            estring equalityBktColumnName = columnInfoRecord.LData.EncryptedEqualityColumnName != null ? new estring(columnInfoRecord.LData.EncryptedEqualityColumnName) : null; 
            estring rangeBktColumnName = columnInfoRecord.LData.EncryptedRangeColumnName != null ? new estring(columnInfoRecord.LData.EncryptedRangeColumnName) : null;
            estring ivColumnName = columnInfoRecord.LData.EncryptedIVColumnName != null ? new estring(columnInfoRecord.LData.EncryptedIVColumnName) : null;

            bool isEncrypted = columnDataType.DataTypeName.ToString() == ENCRYPTED_KEY_WORD;
            bool isNotUnique = ivColumnName != null;

            if (isNotUnique)
            {
                valuesPerColumn[equalityBktColumnName] = new List<Value>();
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
                        var newRangeColumnValue = new Value(_encryptor.CreateRangeBktValue(columnValues.Value[i].ValueToInsert, columnInfoRecord.IV), true);
                        valuesPerColumn[rangeBktColumnName].Add(newRangeColumnValue);
                    }

                    if (isNotUnique)
                    {
                        var equalityBktValue = new Value(_encryptor.CreateEqualityBktValue(columnValues.Value[i].ValueToInsert, columnInfoRecord.IV), true);
                        valuesPerColumn[equalityBktColumnName].Add(equalityBktValue);

                        columnValues.Value[i] = new Value(_encryptor.EncryptNormalValue(columnValues.Value[i].ValueToInsert, columnInfoRecord.IV, out string generatedIV), true);
                        valuesPerColumn[ivColumnName].Add(new Value(generatedIV, true));
                    }
                    else
                    {
                        columnValues.Value[i] = new Value(_encryptor.EncryptUniqueValue(columnValues.Value[i].ValueToInsert, columnInfoRecord.IV), true);
                    }
                }
                else
                {
                    columnValues.Value[i] = new Value(columnValues.Value[i].ValueToInsert, columnDataType.ToString() == TEXT_KEY_WORD);
                }
            }
            return valuesPerColumn;
        }

        private Tuple<IList<ColumnDefinition>, InsertRecordStatement> GetTransformedColumnDefinition(ColumnDefinition columnDefinition, string tableIV, string databaseIV)
        {
            var dataEncrypted = columnDefinition.DataType.DataTypeName == DataTypeEnum.ENCRYPTED;
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
        
        private DeleteRecordStatement CreateDeleteRecordStatementForInfoTable(string iv)
        {
            return new DeleteRecordStatement(INFO_TABLE_NAME, 
                new ComparisonExpression(INFO_TABLE_NAME, NAME, new Value(iv, true), ComparisonExpression.ComparisonOperatorEnum.Equal)
                    );
        }
    }
}
