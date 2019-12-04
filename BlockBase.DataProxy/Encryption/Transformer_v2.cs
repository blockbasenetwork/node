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
        private Encryptor _encryptor;

        //TODO: this will not have a psql conector, instead it will have a classe that will communicate with the producer
        public Transformer_v2(PSqlConnector psqlConnector)
        {
            _psqlConnector = psqlConnector;
            _encryptor = new Encryptor();
        }

        public Builder GetTransformedBuilder(Builder builder)
        {
            var transformedBuilder = new Builder();

            foreach (var entry in builder.SqlStatementsPerDatabase)
            {
                var transformedDatabaseName = GetTransformedDatabaseName(entry.Key);
                transformedBuilder.SqlStatementsPerDatabase[transformedDatabaseName] = new List<ISqlStatement>();

                foreach (var sqlStatement in entry.Value)
                {
                    switch (sqlStatement)
                    {
                        case CreateDatabaseStatement createDatabaseStatement:
                            transformedBuilder.AddStatements(
                                GetTransformedCreateDatabaseStatement(createDatabaseStatement),
                                transformedDatabaseName);
                            break;

                        case DropDatabaseStatement dropDatabaseStatement:
                            transformedBuilder.AddStatement(GetTransformedDropDatabaseStatement(dropDatabaseStatement),
                                transformedDatabaseName);
                            break;

                        case UseDatabaseStatement useDatabaseStatement:
                            transformedBuilder.AddStatement(
                                GetTransformedUseDatabaseStatement(useDatabaseStatement),
                                transformedDatabaseName);
                            break;

                        case CreateTableStatement createTableStatement:
                            transformedBuilder.AddStatements(GetTransformedCreateTableStatement(createTableStatement, entry.Key.Value), transformedDatabaseName);
                            break;

                            //case AbstractAlterTableStatement abstractAlterTableStatement:
                            //    additionalStatements.AddRange(TransformAndGetAdditionalStatements(abstractAlterTableStatement, entry.Key));
                            //    break;

                            //case DropTableStatement dropTableStatement:
                            //    additionalStatements.AddRange(Transform(dropTableStatement, entry.Key));
                            //    break;


                    }
                }
            }
            return transformedBuilder;
        }

        //TODO: fix redundancy
        private List<ISqlStatement> GetTransformedCreateDatabaseStatement(CreateDatabaseStatement createDatabaseStatement)
        {
            //var encrypt
            return new List<ISqlStatement>()
            {
                 new CreateDatabaseStatement(createDatabaseStatement.DatabaseName),
                CreateInfoTable(),

            };
        }
        private DropDatabaseStatement GetTransformedDropDatabaseStatement(DropDatabaseStatement dropDatabaseStatement)
        {
            return new DropDatabaseStatement(dropDatabaseStatement.DatabaseName);
        }
        private UseDatabaseStatement GetTransformedUseDatabaseStatement(UseDatabaseStatement useDatabaseStatement)
        {
            return new UseDatabaseStatement(useDatabaseStatement.DatabaseName);
        }

        private IList<ISqlStatement> GetTransformedCreateTableStatement(CreateTableStatement createTableStatement, string plainDatabaseName)
        {
            var transformedCreateTableStatement = new CreateTableStatement(GetTransformedTableName(createTableStatement.TableName, plainDatabaseName));

            var transformedStatements = new List<ISqlStatement>()
            {
                transformedCreateTableStatement
            };

            foreach (var columnDef in createTableStatement.ColumnDefinitions)
            {
                var additionalColAndInfoInserts = GetTransformedColumnDefinition(columnDef, createTableStatement.TableName, plainDatabaseName);

                ((List<ColumnDefinition>)createTableStatement.ColumnDefinitions).AddRange(additionalColAndInfoInserts.Item1);
                transformedStatements.Add(additionalColAndInfoInserts.Item2);
            }

            return transformedStatements;
        }

        //TODO: add keys and iv
        private Tuple<IList<ColumnDefinition>, InsertRecordStatement> GetTransformedColumnDefinition(ColumnDefinition columnDefinition, estring tableName, string plainDatabaseName)
        {
            var transformedColumnDefinition = new ColumnDefinition(
                GetTransformedColumnName(columnDefinition.ColumnName, tableName.Value, plainDatabaseName),
                columnDefinition.DataType,
                columnDefinition.ColumnConstraints);

            var columnDefinitions = new List<ColumnDefinition>()
            {
                transformedColumnDefinition
            };

            var infoRecord = new InfoRecord()
            {
                Name = transformedColumnDefinition.ColumnName.Value,
                //IsNameEncrypted = columnDefinition.ColumnName.ToEncrypt
            };

            if (columnDefinition.DataType.DataTypeName == DataTypeEnum.ENCRYPTED)
            {
                infoRecord.IsDataEncrypted = true;

                bool isUnique = columnDefinition.ColumnConstraints.Select(c => c.ColumnConstraintType).Contains(ColumnConstraintTypeEnum.PrimaryKey)
                    || columnDefinition.ColumnConstraints.Select(c => c.ColumnConstraintType).Contains(ColumnConstraintTypeEnum.Unique);

                if (!isUnique)
                {
                    columnDefinitions.Add(new ColumnDefinition(
                        CreateIVColumnName(transformedColumnDefinition.ColumnName.Value),
                        new DataType() { DataTypeName = DataTypeEnum.TEXT }
                    ));

                    columnDefinitions.Add(CreateEqualityBucketColumnDefinition(columnDefinition, transformedColumnDefinition, tableName.Value, plainDatabaseName));
                }

                if (columnDefinition.DataType.BucketInfo?.RangeBucketSize != null)
                    columnDefinitions.Add(CreateRangeBucketColumnDefinition(columnDefinition, transformedColumnDefinition, tableName.Value, plainDatabaseName));
            }

            else infoRecord.IsDataEncrypted = false;

            foreach (var foreignKeyClause in columnDefinition.ColumnConstraints.Select(c => c.ForeignKeyClause).Where(f => f != null))
            {
                TransformForeignKeyClause(foreignKeyClause, plainDatabaseName);
            }
            return new Tuple<IList<ColumnDefinition>, InsertRecordStatement>(columnDefinitions, CreateInsertRecordStatementForInfoTable(infoRecord));
        }

        private estring GetTransformedColumnName(estring columnName, string tableName, string plainDatabaseName)
        {
            return GetTransformedName(columnName, _encryptor.EncrypColumnName(plainDatabaseName, tableName, columnName.Value));
        }
        private estring GetTransformedTableName(estring tableName, string plainDatabaseName)
        {
            return GetTransformedName(tableName, _encryptor.EncrypTableName(plainDatabaseName, tableName.Value));
        }
        private estring GetTransformedDatabaseName(estring databaseName)
        {
            return GetTransformedName(databaseName, _encryptor.EncryptDatabaseName(databaseName.Value));
        }
        private estring GetTransformedName(estring name, string encryptedValue)
        {
            return new estring(name.ToEncrypt ? _separatingChar + encryptedValue : name.Value, false);
        }

        private estring CreateIVColumnName(string columnName)
        {
            return new estring(_ivPrefix + _separatingChar + columnName, false);
        }
        private estring CreateEqualityBktColumnName(string columnName, int? size, string plainDatabaseName, string plainTableName)
        {
            var bucketColumnNameString = columnName.Substring(1, 4) + _separatingChar + size;
            var encryptedSizeAndRange = _encryptor.EncrypColumnName(plainDatabaseName, plainTableName, bucketColumnNameString);
            return new estring(_equalityBucketPrefix + _separatingChar + columnName + _separatingChar + encryptedSizeAndRange, false);
        }
        private estring CreateRangeBktColumnName(string columnName, int? size, int? min, int? max, string plainDatabaseName, string plainTableName)
        {
            var bucketColumnNameString = columnName.Substring(1, 4) + _separatingChar + size + _separatingChar + min + _separatingChar + max;
            var encryptedSizeAndRange = _encryptor.EncrypColumnName(plainDatabaseName, plainTableName, bucketColumnNameString);

            return new estring(_rangeBucketPrefix + _separatingChar + columnName + _separatingChar + encryptedSizeAndRange, false);
        }

        private ColumnDefinition CreateEqualityBucketColumnDefinition(ColumnDefinition columnDef, ColumnDefinition transformedColumnDef, string plainTableName, string plainDatabaseName)
        {
            var equalityBktColumnName = CreateEqualityBktColumnName(transformedColumnDef.ColumnName.Value, columnDef.DataType.BucketInfo.EqualityBucketSize,
                 plainDatabaseName, plainTableName);

            return new ColumnDefinition()
            {
                ColumnName = equalityBktColumnName,
                DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                ColumnConstraints = new List<ColumnConstraint>()
            };

        }
        private ColumnDefinition CreateRangeBucketColumnDefinition(ColumnDefinition columnDef, ColumnDefinition transformedColumnDef, string plainTableName, string plainDatabaseName)
        {
            var rangeBktColumnName = CreateRangeBktColumnName(transformedColumnDef.ColumnName.Value, columnDef.DataType.BucketInfo.RangeBucketSize,
                 columnDef.DataType.BucketInfo.BucketMinRange, columnDef.DataType.BucketInfo.BucketMaxRange, plainDatabaseName, plainTableName);

            return new ColumnDefinition()
            {
                ColumnName = rangeBktColumnName,
                DataType = new DataType() { DataTypeName = DataTypeEnum.TEXT },
                ColumnConstraints = new List<ColumnConstraint>()
            };
        }

        private ForeignKeyClause TransformForeignKeyClause(ForeignKeyClause foreignKeyClause, string plainDatabaseName)
        {
            var transformedForeignKeyClause = new ForeignKeyClause(GetTransformedTableName(foreignKeyClause.TableName, plainDatabaseName));
            foreach (var columnName in foreignKeyClause.ColumnNames)
            {
                transformedForeignKeyClause.ColumnNames.Add(GetTransformedColumnName(columnName, foreignKeyClause.TableName.Value, plainDatabaseName));
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
        private InfoRecord CreateDatabaseInfoRecord(estring databaseName)
        {
            var infoRecord = new InfoRecord();

            return infoRecord;




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
                    { KEY_READ, new List<Value>() { new Value(infoRecord.KeyRead, true) }  },
                    { KEY_MANAGE, new List<Value>() { new Value(infoRecord.KeyManage, true) }  },
                    { KEY_READ, new List<Value>() { new Value((infoRecord.IsDataEncrypted + "").ToUpper(), false) }  }
                }
            };
        }
    }
}
