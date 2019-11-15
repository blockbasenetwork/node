using BlockBase.Domain.Database;
using BlockBase.Domain.Database.Columns;
using BlockBase.Domain.Database.Constants;
using BlockBase.Domain.Database.Operations;
using BlockBase.Domain.Database.Records;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.Utils.Crypto;
using Org.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Numerics;
using System.Text;
using Wiry.Base32;

namespace BlockBase.DataProxy.Crypto
{
    public class EncryptSqlOperation
    {
        private const int PRIMARY_COLUMN_ID = 1;
        private const int FOREIGN_COLUMN_ID = 2;
        private const int UNIQUE_COLUMN_ID = 3;
        private const int RANGE_COLUMN_ID = 4;
        private const int NORMAL_COLUMN_ID = 5;
        private const int AES_BLOCK_SIZE = 16;
        private byte[] _masterKey;
        private string _proxyDatabaseName { get; set; }
        private byte[] _initializationVector;
        private DatabaseConstants _databaseConstants;

        public EncryptSqlOperation(byte[] masterKey, byte[] initializationVector, string databaseName, IConnector connector)
        {
            _masterKey = masterKey;
            _initializationVector = initializationVector;
            _proxyDatabaseName = databaseName;
            _databaseConstants = connector.GetDatabaseConstants();
        }

        public CreateTableOperation EncryptCreateTable(CreateTableOperation createTable, out List<ISqlOperation> bucketInfoOperations)
        {
            var result = new CreateTableOperation();
            var encryptedColumns = new List<Column>();
            bucketInfoOperations = new List<ISqlOperation>();

            var encryptedTableName = _databaseConstants.TABLE_NAME_PREFIX + GetEncryptedTableName(createTable.Table.TableName);

            foreach (Column column in createTable.Table.Columns)
            {
                var encryptedColumnName = GetEncryptedColumnName(createTable.Table.TableName, column.Name);
                encryptedColumns.AddRange(CreateColumns(column, encryptedTableName, createTable.Table.TableName));
                bucketInfoOperations.AddRange(InsertBucketInfo(column, encryptedTableName, encryptedColumnName));
            }
            var newTable = new Table(encryptedTableName, encryptedColumns);
            result.Table = newTable;
            return result;
        }

        private List<Column> CreateColumns(Column column, string encryptedTableName, string tableName)
        {
            var encryptedColumns = new List<Column>(); ;
            Column newColumn = (Column)column.Clone();
            var encryptedColumnName = GetEncryptedColumnName(tableName, column.Name);
            newColumn.Name = _databaseConstants.MAIN_ONION_PREFIX + (encryptedColumnName);
            newColumn.Size = (column.Size / 16) + 1;
            var isForeign = newColumn is ForeignColumn;
            var isPrimary = newColumn is PrimaryColumn;
            var isUnique = newColumn is UniqueColumn;
            if (newColumn is ForeignColumn foreignColumn)
            {
                foreignColumn.ForeignColumnName = _databaseConstants.MAIN_ONION_PREFIX + (GetEncryptedColumnName(foreignColumn.ForeignTableName, foreignColumn.ForeignColumnName));
                foreignColumn.ForeignTableName = _databaseConstants.TABLE_NAME_PREFIX + (GetEncryptedTableName(foreignColumn.ForeignTableName));
            }
            encryptedColumns.Add(newColumn);

            //IV COLUMN AND BUCKET
            if (!isPrimary && !isForeign && !isUnique)
            {
                var IVName = _databaseConstants.IV_NAME_PREFIX + encryptedColumnName;
                var IVColumn = new Column(IVName, SqlDbType.VarBinary, false, 32);

                var BucketName = _databaseConstants.BUCKET_COLUMN_PREFIX + encryptedColumnName;
                var BucketColumn = new Column(BucketName, SqlDbType.VarBinary, false, 256);

                encryptedColumns.Add(IVColumn);
                encryptedColumns.Add(BucketColumn);
            }
            return encryptedColumns;
        }

        private List<ISqlOperation> InsertBucketInfo(Column column, string encryptedTableName, string encryptedColumnName)
        {
            var columnInfoRecords = new InsertRecordOperation();
            var onion = new Onion();
            var id = Guid.NewGuid();
            columnInfoRecords.TableName = _databaseConstants.COLUMN_INFO_TABLE;
            Record recordColumnName = new StringRecord(_databaseConstants.COLUMN_NAME, _databaseConstants.MAIN_ONION_PREFIX + (encryptedColumnName));
            Record recordTableName = new StringRecord(_databaseConstants.COLUMN_INFO_TABLE_NAMES, encryptedTableName);
            Record recordId = new GuidRecord(_databaseConstants.ID, id);
            var recordList = new List<Record> { recordColumnName, recordTableName, recordId };
            var result = new List<ISqlOperation>();
            Record recordType;
            InsertRecordOperation typeOperation;

            if (column is RangeColumn rangeColumn)
            {
                recordType = new IntRecord(_databaseConstants.COLUMN_TYPE_ID, RANGE_COLUMN_ID);
                typeOperation = new InsertRecordOperation { TableName = _databaseConstants.RANGE_COLUMNS_TABLE_NAME };
                var encryptMaxRange = onion.CreateOnion(Encoding.ASCII.GetBytes(rangeColumn.MaxRange.ToString()), _masterKey, _initializationVector, _databaseConstants.RANGE_COLUMNS_TABLE_NAME, _databaseConstants.MAXRANGE_COLUMN, out byte[] maxRangeIV);
                var encryptNumberOfBuckets = onion.CreateOnion(Encoding.ASCII.GetBytes(rangeColumn.NumberOfBuckets.ToString()), _masterKey, _initializationVector, _databaseConstants.RANGE_COLUMNS_TABLE_NAME, _databaseConstants.BUCKET_SIZE_COLUMN_NAME, out byte[] NumberOfBucketsIV);
                var rangeRecordList = new List<Record>
                {
                    new VarBinaryRecord(_databaseConstants.MAXRANGE_COLUMN, encryptMaxRange),
                    new VarBinaryRecord(_databaseConstants.MAX_RANGE_IV_COLUMN, maxRangeIV),
                    new VarBinaryRecord(_databaseConstants.BUCKET_SIZE_COLUMN_NAME, encryptNumberOfBuckets),
                    new VarBinaryRecord(_databaseConstants.BUCKET_SIZE_IV_COLUMN_NAME, NumberOfBucketsIV),
                    new BoolRecord(_databaseConstants.CANBENEGATIVE_COLUMN, rangeColumn.CanBeNegative),
                    new GuidRecord(_databaseConstants.COLUMN_ID_NAME, id)
                };
                typeOperation.ValuesToInsert = rangeRecordList;
            }
            else if (column is PrimaryColumn)
            {
                recordType = new IntRecord(_databaseConstants.COLUMN_TYPE_ID, PRIMARY_COLUMN_ID);
                typeOperation = new InsertRecordOperation
                {
                    TableName = _databaseConstants.PRIMARY_COLUMNS_TABLE_NAME,
                    ValuesToInsert = new List<Record> { new GuidRecord(_databaseConstants.COLUMN_ID_NAME, id) }
                };
            }
            else if (column is ForeignColumn)
            {
                recordType = new IntRecord(_databaseConstants.COLUMN_TYPE_ID, FOREIGN_COLUMN_ID);
                typeOperation = new InsertRecordOperation
                {
                    TableName = _databaseConstants.FOREIGN_COLUMNS_TABLE_NAME,
                    ValuesToInsert = new List<Record> { new GuidRecord(_databaseConstants.COLUMN_ID_NAME, id) }
                };
            }
            else if (column is UniqueColumn)
            {
                recordType = new IntRecord(_databaseConstants.COLUMN_TYPE_ID, UNIQUE_COLUMN_ID);
                typeOperation = new InsertRecordOperation
                {
                    TableName = _databaseConstants.UNIQUE_COLUMNS_TABLE_NAME,
                    ValuesToInsert = new List<Record> { new GuidRecord(_databaseConstants.COLUMN_ID_NAME, id) }
                };
            }
            else // NORMAL COLUMN
            {
                NormalColumn normalColumn = (NormalColumn)column;
                var encryptNumberOfBuckets = onion.CreateOnion(Encoding.ASCII.GetBytes(normalColumn.NumberOfBuckets.ToString()), _masterKey, _initializationVector, _databaseConstants.RANGE_COLUMNS_TABLE_NAME, _databaseConstants.BUCKET_SIZE_COLUMN_NAME, out byte[] NumberOfBucketsIV);
                recordType = new IntRecord(_databaseConstants.COLUMN_TYPE_ID, NORMAL_COLUMN_ID);
                typeOperation = new InsertRecordOperation
                {
                    TableName = _databaseConstants.NORMAL_COLUMNS_TABLE_NAME,
                    ValuesToInsert = new List<Record>
                    {
                        new GuidRecord(_databaseConstants.COLUMN_ID_NAME, id),
                        new VarBinaryRecord(_databaseConstants.BUCKET_SIZE_COLUMN_NAME, encryptNumberOfBuckets),
                        new VarBinaryRecord(_databaseConstants.BUCKET_SIZE_IV_COLUMN_NAME, NumberOfBucketsIV),
                    }
                };
            }
            recordList.Add(recordType);
            columnInfoRecords.ValuesToInsert = recordList;
            result.Add(columnInfoRecords);
            result.Add(typeOperation);
            return result;
        }

        public List<CreateColumnOperation> EncryptCreateColumn(CreateColumnOperation createColumn, out List<ISqlOperation> bucketInfoOperations)
        {
            var encryptedCreateColumnList = new List<CreateColumnOperation>();
            bucketInfoOperations = new List<ISqlOperation>();

            var encryptedtableName = _databaseConstants.TABLE_NAME_PREFIX + (GetEncryptedTableName(createColumn.TableName));
            var encryptedColumns = CreateColumns(createColumn.Column, encryptedtableName, createColumn.TableName);

            var encryptedColumnName = GetEncryptedColumnName(createColumn.TableName, createColumn.Column.Name);
            var bucketInfo = InsertBucketInfo(createColumn.Column, encryptedtableName, encryptedColumnName);
            foreach (ISqlOperation operation in bucketInfo)
                bucketInfoOperations.Add(operation);

            foreach (Column column in encryptedColumns)
            {
                CreateColumnOperation encryptedCreateColumn = new CreateColumnOperation { Column = column, TableName = encryptedtableName };
                encryptedCreateColumnList.Add(encryptedCreateColumn);
            }
            return encryptedCreateColumnList;
        }
        public int GetMaxRange(byte[] value, byte[] iv)
        {
            var onion = new Onion();
            var detLayer = onion.DecryptRandomLayer(value, _masterKey, _databaseConstants.RANGE_COLUMNS_TABLE_NAME, _databaseConstants.MAXRANGE_COLUMN, iv);
            var byteArrayValue = onion.DecryptDeterministicLayer(detLayer, _masterKey, _initializationVector, _databaseConstants.RANGE_COLUMNS_TABLE_NAME, _databaseConstants.MAXRANGE_COLUMN);
            var maxRange = int.Parse(Encoding.ASCII.GetString(byteArrayValue));
            return maxRange;
        }
        public int GetBucketSize(byte[] value, byte[] iv)
        {
            var onion = new Onion();
            var detLayer = onion.DecryptRandomLayer(value, _masterKey, _databaseConstants.RANGE_COLUMNS_TABLE_NAME, _databaseConstants.BUCKET_SIZE_COLUMN_NAME, iv);
            var byteArrayValue = onion.DecryptDeterministicLayer(detLayer, _masterKey, _initializationVector, _databaseConstants.RANGE_COLUMNS_TABLE_NAME, _databaseConstants.BUCKET_SIZE_COLUMN_NAME);
            var bucketSize = int.Parse(Encoding.ASCII.GetString(byteArrayValue));
            return bucketSize;
        }
        public string GetEncryptedTableName(string tableName)
        {
            var location = _proxyDatabaseName;
            byte[] deterministicLayerKey = GenerateKeys(location, out byte[] generatedIV);
            var columnNameByte = AES256.EncryptWithCBC(Encoding.ASCII.GetBytes(tableName), deterministicLayerKey, generatedIV);
            return Base32Encoding.ZBase32.GetString(columnNameByte);
        }
        public string GetEncryptedColumnName(string tableName, string columnName)
        {
            var location = _proxyDatabaseName + tableName;
            byte[] deterministicLayerKey = GenerateKeys(location, out byte[] generatedIV);
            var columnNameByte = AES256.EncryptWithCBC(Encoding.ASCII.GetBytes(columnName), deterministicLayerKey, generatedIV);
            return Base32Encoding.ZBase32.GetString(columnNameByte);
        }
        public string DecryptTableName(string name)
        {
            var location = _proxyDatabaseName;
            byte[] deterministicLayerKey = GenerateKeys(location, out byte[] generatedIV);
            name = name.Substring(_databaseConstants.TABLE_NAME_PREFIX.Length);
            var columnNameByte = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(name), deterministicLayerKey, generatedIV);
            return Encoding.ASCII.GetString(columnNameByte);
        }
        public string DecryptColumName(string tableName, string name)
        {
            var location = _proxyDatabaseName + tableName;
            byte[] deterministicLayerKey = GenerateKeys(location, out byte[] generatedIV);
            name = name.Substring(_databaseConstants.MAIN_ONION_PREFIX.Length);
            var columnNameByte = AES256.DecryptWithCBC(Base32Encoding.ZBase32.ToBytes(name), deterministicLayerKey, generatedIV);
            return Encoding.ASCII.GetString(columnNameByte);
        }
        private byte[] GenerateKeys(string location, out byte[] generatedIV)
        {
            var locationInBytes = Encoding.ASCII.GetBytes(location); // detLayerKey = AESm(sha1(tableName,columnName,layer));
            var hash = Utils.Crypto.Utils.SHA256(locationInBytes);
            var key = AES256.EncryptWithECB(hash, _masterKey);

            var LocationAndIVArray = Utils.Crypto.Utils.ConcatenateByteArray(locationInBytes, _initializationVector); // deterministicIV = AESm(sha1(tableName,columnName,layer,masterIV));
            generatedIV = Utils.Crypto.Utils.Sha1AndResize(LocationAndIVArray, AES_BLOCK_SIZE);
            return key;
        }

        public List<ISqlOperation> EncryptDeleteColumn(DeleteColumnOperation deleteColumn)
        {
            //TODO: not removing bucket column?
            var result = new DeleteColumnOperation();
            var columnsToDelete = new List<ISqlOperation>();
            result.TableName = _databaseConstants.TABLE_NAME_PREFIX + GetEncryptedTableName(deleteColumn.TableName);
            var columnName = GetEncryptedColumnName(deleteColumn.TableName, deleteColumn.ColumnName);
            result.ColumnName = _databaseConstants.MAIN_ONION_PREFIX + columnName;

            var IVColumn = new DeleteColumnOperation { TableName = result.TableName, ColumnName = _databaseConstants.IV_NAME_PREFIX + columnName };

            columnsToDelete.Add(result);
            columnsToDelete.Add(IVColumn);
            return columnsToDelete;
        }

        public DeleteRecordOperation EncryptDeleteRecordWithoutEncryptedIdentifier(DeleteRecordOperation deleteRecord, ColumnType type)
        {
            var onion = new Onion();
            var deleteValue = new DeleteRecordOperation
            {
                TableName = _databaseConstants.TABLE_NAME_PREFIX + GetEncryptedTableName(deleteRecord.TableName),
                ColumnName = _databaseConstants.MAIN_ONION_PREFIX + GetEncryptedColumnName(deleteRecord.TableName, deleteRecord.ColumnName)
            };
            if (type == ColumnType.PrimaryColumn || type == ColumnType.ForeignColumn)
            {
                var result = Guid.TryParse(deleteRecord.Value, out Guid dummyGuid);

                if (result) deleteValue.Value = deleteRecord.Value;
                else throw new Exception("Identifier must be a GUID");
            }
            else
            {
                var detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(deleteRecord.Value), _masterKey, _initializationVector, deleteRecord.TableName, deleteRecord.ColumnName);
                deleteRecord.Value = StringHex(detLayer);
            }
            return deleteValue;
        }
        public string StringHex(byte[] detlay)
        {
            return "0x" + BitConverter.ToString(detlay).Replace("-", "");
        }

        public DeleteTableOperation EncryptDeleteTable(DeleteTableOperation deleteTable)
        {
            var result = new DeleteTableOperation { TableName = _databaseConstants.TABLE_NAME_PREFIX + GetEncryptedTableName(deleteTable.TableName) };
            return result;
        }

        public InsertRecordOperation EncryptInsertRecord(InsertRecordOperation insertRecord)
        {
            var result = new InsertRecordOperation { TableName = _databaseConstants.TABLE_NAME_PREFIX + GetEncryptedTableName(insertRecord.TableName) };
            List<Record> encryptedRecords = EncryptRecords(insertRecord.ValuesToInsert, insertRecord.TableName);
            result.ValuesToInsert = encryptedRecords;
            return result;
        }

        private List<Record> EncryptRecords(List<Record> valuesToInsert, string tableName)
        {
            var encryptedColumns = new List<Record>();
            foreach (Record record in valuesToInsert)
            {
                Onion onion = new Onion();
                var columnName = GetEncryptedColumnName(tableName, record.Column);
                var mainColumnName = _databaseConstants.MAIN_ONION_PREFIX + columnName;
                if (record.BucketSize > 0)
                {
                    var encryptedRecord = new VarBinaryRecord { Column = mainColumnName };
                    //TODO : this can be optimized: instead of create the full onion, first get detLayer then random Layer
                    byte[] valueOnion = onion.CreateOnion(Encoding.ASCII.GetBytes(record.GetValue()), _masterKey, _initializationVector, tableName, record.Column, out byte[] randomIV);
                    encryptedRecord.Value = valueOnion;
                    byte[] detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(record.GetValue()), _masterKey, _initializationVector, tableName, record.Column);
                    byte[] bucket;
                    Record encryptedIVRecord = new VarBinaryRecord
                    {
                        Column = _databaseConstants.IV_NAME_PREFIX + columnName,
                        Value = randomIV
                    };

                    if (record.ValueMaxRange != 0) bucket = GetRangeBucket(record.BucketSize, record.ValueMaxRange, record.GetValue(), tableName, record.Column);
                    else bucket = GetBucket(record.BucketSize, detLayer);

                    Record encryptedBucketRecord = new VarBinaryRecord { Column = _databaseConstants.BUCKET_COLUMN_PREFIX + columnName, Value = bucket };
                    encryptedColumns.Add(encryptedBucketRecord);
                    encryptedColumns.Add(encryptedIVRecord);
                    encryptedColumns.Add(encryptedRecord);
                }
                else if (record.Type == ColumnType.PrimaryColumn || record.Type == ColumnType.ForeignColumn)
                {
                    var guidRecord = (GuidRecord)record;
                    var encryptedRecord = new GuidRecord { Column = _databaseConstants.MAIN_ONION_PREFIX + columnName, Value = guidRecord.Value };
                    encryptedColumns.Add(encryptedRecord);
                }
                else if (record.Type == ColumnType.UniqueColumn)
                {
                    byte[] detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(record.GetValue()), _masterKey, _initializationVector, tableName, record.Column);
                    var encryptedRecord = new VarBinaryRecord { Column = mainColumnName, Value = detLayer };
                    encryptedColumns.Add(encryptedRecord);
                }
            }
            return encryptedColumns;
        }

        public byte[] GetRangeBucket(int bucketSize, int valueMaxRange, string value, string tableName, string column)
        {
            var valueToInt = int.Parse(value);
            var rangePerBucket = valueMaxRange / bucketSize;
            var bucket = valueToInt / rangePerBucket;
            var bucketLimit = bucket * rangePerBucket;
            Onion onion = new Onion();
            var detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(bucketLimit.ToString()), _masterKey, _initializationVector, tableName, column);
            return Utils.Crypto.Utils.SHA256(detLayer);
        }
        public List<string> GetBucketsHigherThan(int bucketSize, int valueMaxRange, string value, string tableName, string column, bool canBeNegative)
        {
            var result = new List<string>();
            Onion onion = new Onion();
            var valueToInt = int.Parse(value);
            if (canBeNegative)
            {
                var minValue = (valueMaxRange * -1);
                if (valueToInt < minValue) valueToInt = minValue;
            }
            else if (valueToInt < 0) valueToInt = 0;

            var rangePerBucket = valueMaxRange / bucketSize;
            var bucket = valueToInt / rangePerBucket;
            for (int i = bucket; i < bucketSize; i++)
            {
                var bucketLimit = i * rangePerBucket;
                var detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(bucketLimit.ToString()), _masterKey, _initializationVector, tableName, column);
                var hashDetLayer = Utils.Crypto.Utils.SHA256(detLayer);
                result.Add(StringHex(hashDetLayer));
            }
            return result;
        }
        public List<string> GetBucketsLessThan(int bucketSize, int valueMaxRange, string value, string tableName, string column, bool canBeNegative)
        {
            var result = new List<string>();
            Onion onion = new Onion();
            var valueToInt = int.Parse(value);
            if (valueToInt > valueMaxRange)
                valueToInt = valueMaxRange;

            var rangePerBucket = valueMaxRange / bucketSize;
            var bucket = valueToInt / rangePerBucket;
            var minBucket = 0;
            if (canBeNegative)
                minBucket = bucketSize * -1;

            for (int i = bucket; i >= minBucket; i--)
            {
                var bucketLimit = i * rangePerBucket;
                var detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(bucketLimit.ToString()), _masterKey, _initializationVector, tableName, column);
                var hashDetLayer = Utils.Crypto.Utils.SHA256(detLayer);
                result.Add(StringHex(hashDetLayer));
            }
            return result;
        }
        public byte[] GetBucket(int bucketSize, byte[] detLayer)
        {
            var hash = Utils.Crypto.Utils.SHA256(detLayer);
            var integerHash = new BigInteger(hash);
            var bucket = BigInteger.Remainder(integerHash, bucketSize);
            bucket = BigInteger.Abs(bucket);
            return bucket.ToByteArray();
        }

        public UpdateRecordOperation EncryptUpdateRecordWithoutEncryptedIdentifier(UpdateRecordOperation updateRecord, ColumnType type)
        {
            var encryptedUpdateRecord = new UpdateRecordOperation();
            var onion = new Onion();

            encryptedUpdateRecord.TableName = _databaseConstants.TABLE_NAME_PREFIX + GetEncryptedTableName(updateRecord.TableName);
            encryptedUpdateRecord.IdentifierColumn = _databaseConstants.MAIN_ONION_PREFIX + GetEncryptedColumnName(updateRecord.TableName, updateRecord.IdentifierColumn);
            if (type == ColumnType.PrimaryColumn || type == ColumnType.ForeignColumn)
            {
                var result = Guid.TryParse(updateRecord.IdentifierValue, out Guid dummyGuid);

                if (result) encryptedUpdateRecord.IdentifierValue = updateRecord.IdentifierValue;
                else throw new Exception("Identifier must be a GUID");
            }
            else
            {
                var detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(updateRecord.IdentifierValue), _masterKey, _initializationVector, updateRecord.TableName, updateRecord.IdentifierColumn);
                encryptedUpdateRecord.IdentifierValue = StringHex(detLayer);
            }
            encryptedUpdateRecord.ValuesToUpdate = EncryptRecords(updateRecord.ValuesToUpdate, updateRecord.TableName);
            return encryptedUpdateRecord;
        }
        public UpdateRecordOperation EncryptUpdateRecordWithEncryptedIdentifier(UpdateRecordOperation updateRecord, string tableName)
        {
            var encryptedUpdateRecord = new UpdateRecordOperation();
            var onion = new Onion();

            encryptedUpdateRecord.TableName = updateRecord.TableName;
            encryptedUpdateRecord.IdentifierColumn = updateRecord.IdentifierColumn;
            encryptedUpdateRecord.IdentifierValue = updateRecord.IdentifierValue;
            encryptedUpdateRecord.ValuesToUpdate = EncryptRecords(updateRecord.ValuesToUpdate, tableName);
            return encryptedUpdateRecord;
        }
    }
}
