using BlockBase.Domain.Database;
using BlockBase.Domain.Database.Operations;
using BlockBase.DataPersistence.Utils;
using BlockBase.DataProxy.Crypto;
using BlockBase.Utils.Crypto;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Linq;
using BlockBase.Domain.Database.QueryResults;
using BlockBase.Domain.Database.Records;
using BlockBase.Domain.Database.Columns;
using BlockBase.DataPersistence.Sidechain.Connectors;

namespace BlockBase.DataProxy
{
    public class SqlOperationsEncryption
    {

        private const string APOSTROPHE = "'";
        private const int WHEREEQUALS = 0;
        private const int WHEREHIGHER = 1;
        private const int WHERELESS = 2;
        private const int WHERELESSOREQUAL = 3;
        private const int WHEREHIGHEROREQUAL = 4;



        private byte[] _masterKey;
        private byte[] _iv;
        private string _proxyDatabaseName;
        private IConnector _connector;

        public SqlOperationsEncryption(byte[] masterKey, byte[] iv, IConnector connector, string database)
        {
            _masterKey = masterKey;
            _iv = iv;
            _connector = connector;
            _proxyDatabaseName = database;
        }
        public CreateTableOperation CreateTable(CreateTableOperation createTable, out List<ISqlOperation> bucketInfoOperations)
        {
            var encryptionSqlOperation = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            return encryptionSqlOperation.EncryptCreateTable(createTable, out bucketInfoOperations);
        }
        public List<CreateColumnOperation> CreateColumn(CreateColumnOperation createColumn, out List<ISqlOperation> bucketInfoOperations)
        {
            var encryptionSqlOperation = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            return encryptionSqlOperation.EncryptCreateColumn(createColumn, out bucketInfoOperations);
        }
        public List<ISqlOperation> DeleteColumn(DeleteColumnOperation deleteColumn)
        {
            var encryptionSqlOperation = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            return encryptionSqlOperation.EncryptDeleteColumn(deleteColumn);
        }
        public ISqlOperation DeleteTable(DeleteTableOperation deleteTable)
        {
            var encryptionSqlOperation = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            return encryptionSqlOperation.EncryptDeleteTable(deleteTable);
        }
        public ISqlOperation InsertRecord(InsertRecordOperation insertRecord)
        {
            var sqlOperationEncryption = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var tableName = _connector.GetTableNameWithPrefix(sqlOperationEncryption.GetEncryptedTableName(insertRecord.TableName));
            var bucketSizesAndNames = _connector.GetAllBucketSize(tableName, _proxyDatabaseName);
            var columnsNames = (StringQueryResult)bucketSizesAndNames[0];

            if (columnsNames.Values.Count == 0)
            {
                throw new Exception("Values to insert are not valid.");
            }
            var indexBucketSizes = new Dictionary<string, Tuple<ColumnType, int?, int?>>(); //TODO: this should be a class and not a tuple
            var columnTypes = (IntQueryResult)bucketSizesAndNames[1];
            var NormalColumnsBucketSize = (BinaryQueryResult)bucketSizesAndNames[2];
            var NormalColumnsIVBucketSize = (BinaryQueryResult)bucketSizesAndNames[3];
            var RangeColumnsBucketSize = (BinaryQueryResult)bucketSizesAndNames[4];
            var RangeColumnsIVBucketSize = (BinaryQueryResult)bucketSizesAndNames[5];
            var MaxRangeColumns = (BinaryQueryResult)bucketSizesAndNames[6];
            var MaxRangeColumnsIV = (BinaryQueryResult)bucketSizesAndNames[7];
            for (int i = 0; i < columnsNames.Values.Count; i++)
            {
                var columnType = (ColumnType)columnTypes.Values[i];

                if (columnType == ColumnType.RangeColumn)
                {
                    var bucketSize = sqlOperationEncryption.GetBucketSize(RangeColumnsBucketSize.Values[i], RangeColumnsIVBucketSize.Values[i]);
                    var maxRange = sqlOperationEncryption.GetMaxRange(MaxRangeColumns.Values[i], MaxRangeColumnsIV.Values[i]);
                    indexBucketSizes[columnsNames.Values[i]] = new Tuple<ColumnType, int?, int?>(columnType, bucketSize, maxRange);
                }
                else if (columnType == ColumnType.NormalColumn)
                {
                    var bucketSize = sqlOperationEncryption.GetBucketSize(NormalColumnsBucketSize.Values[i], NormalColumnsIVBucketSize.Values[i]);
                    indexBucketSizes[columnsNames.Values[i]] = new Tuple<ColumnType, int?, int?>(columnType, bucketSize, null);
                }
                else
                {
                    indexBucketSizes[columnsNames.Values[i]] = new Tuple<ColumnType, int?, int?>(columnType, null, null);
                }
            }
            foreach (Record record in insertRecord.ValuesToInsert)
            {
                var columnName = sqlOperationEncryption.GetEncryptedColumnName(insertRecord.TableName, record.Column);
                var mainColumnName = _connector.GetColumnNameWithPrefix(columnName);
                var type = indexBucketSizes[mainColumnName].Item1;
                var bucketSize = indexBucketSizes[mainColumnName].Item2;
                var valueMaxRange = indexBucketSizes[mainColumnName].Item3;
                if (valueMaxRange != null)
                {
                    record.ValueMaxRange = (int)valueMaxRange;
                }
                if (bucketSize != null)
                {
                    record.BucketSize = (int)bucketSize;
                }
                record.Type = type;
            }

            var encryptionSqlOperation = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var encryptedOperation = encryptionSqlOperation.EncryptInsertRecord(insertRecord);
            return encryptedOperation;
        }
        //If indentifier is not unique it takes much longer
        public List<ISqlOperation> UpdateRecords(UpdateRecordOperation updateRecord)
        {
            var sqlOperationEncryption = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var tableName = _connector.GetTableNameWithPrefix(sqlOperationEncryption.GetEncryptedTableName(updateRecord.TableName));
            ColumnType columnType;
            var onion = new Onion();
            foreach (Record record in updateRecord.ValuesToUpdate)
            {
                var valueToUpdateColumnName = sqlOperationEncryption.GetEncryptedColumnName(updateRecord.TableName, record.Column);
                var mainColumnName = _connector.GetColumnNameWithPrefix(valueToUpdateColumnName);
                var typeAndIdRecord = _connector.GetColumnTypeAndId(mainColumnName, tableName, _proxyDatabaseName);
                record.Type = typeAndIdRecord.Item1;
                columnType = typeAndIdRecord.Item1;
                if (columnType == ColumnType.RangeColumn)
                {
                    var valueMaxRange = _connector.GetBucketMaxRange(typeAndIdRecord.Item2, _proxyDatabaseName);
                    int maxRange = sqlOperationEncryption.GetMaxRange(valueMaxRange.Item1, valueMaxRange.Item2);
                    record.ValueMaxRange = maxRange;
                }
                if (columnType == ColumnType.RangeColumn || columnType == ColumnType.NormalColumn)
                {
                    var bucketSizeAndIV = _connector.GetBucket(typeAndIdRecord.Item2, typeAndIdRecord.Item1, _proxyDatabaseName);
                    int bucketSize = sqlOperationEncryption.GetBucketSize(bucketSizeAndIV.Item1, bucketSizeAndIV.Item2);
                    record.BucketSize = bucketSize;
                }

            }

            var queryBuilder = new QueryBuilder();
            var columnName = sqlOperationEncryption.GetEncryptedColumnName(updateRecord.TableName, updateRecord.IdentifierColumn);
            var valueColumn = _connector.GetColumnNameWithPrefix(columnName);
            var typeAndId = _connector.GetColumnTypeAndId(valueColumn, tableName, _proxyDatabaseName);
            columnType = typeAndId.Item1;
            List<ISqlOperation> result = new List<ISqlOperation>();
            if (columnType != ColumnType.RangeColumn && columnType != ColumnType.NormalColumn)
            {
                result.Add(sqlOperationEncryption.EncryptUpdateRecordWithoutEncryptedIdentifier(updateRecord, columnType));
            }
            else
            {
                queryBuilder = new QueryBuilder();
                var primaryKey = _connector.GetPrimaryKey(tableName, _proxyDatabaseName);
                var detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(updateRecord.IdentifierValue), _masterKey, _iv, updateRecord.TableName, updateRecord.IdentifierColumn);
                var bucketSizeAndIV = _connector.GetBucket(typeAndId.Item2, typeAndId.Item1, _proxyDatabaseName);
                int bucketSize = sqlOperationEncryption.GetBucketSize(bucketSizeAndIV.Item1, bucketSizeAndIV.Item2);
                var bucket = sqlOperationEncryption.GetBucket(bucketSize, detLayer);
                var list = _connector.GetIdentifiersWithBucket(primaryKey, tableName, valueColumn, columnName, bucket, _proxyDatabaseName);
                List<string> identifiersToUpdate = new List<string>();
                foreach (Tuple<string, byte[], byte[]> value in list)
                {
                    detLayer = onion.DecryptRandomLayer(value.Item2, _masterKey, updateRecord.TableName, updateRecord.IdentifierColumn, value.Item3);
                    var onionValue = onion.DecryptDeterministicLayer(detLayer, _masterKey, _iv, updateRecord.TableName, updateRecord.IdentifierColumn);
                    var trueValue = Encoding.ASCII.GetString(onionValue);
                    if (trueValue == updateRecord.IdentifierValue)
                    {
                        identifiersToUpdate.Add(value.Item1);
                    }
                }
                foreach (string indentifier in identifiersToUpdate)
                {
                    var updateRecordOperation = (UpdateRecordOperation)updateRecord.Clone();
                    updateRecordOperation.IdentifierColumn = primaryKey;
                    updateRecordOperation.TableName = tableName;
                    updateRecordOperation.IdentifierValue = APOSTROPHE + indentifier + APOSTROPHE; // BECAUSE IS A GUID
                    updateRecordOperation = sqlOperationEncryption.EncryptUpdateRecordWithEncryptedIdentifier(updateRecordOperation, updateRecord.TableName);
                    result.Add(updateRecordOperation);
                }
            }
            return result;
        }

        //If indentifier is not unique it takes much longer
        public List<ISqlOperation> DeleteRecord(DeleteRecordOperation deleteRecord)
        {
            var sqlOperationEncryption = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var columnName = sqlOperationEncryption.GetEncryptedColumnName(deleteRecord.TableName, deleteRecord.ColumnName);

            var valueColumn = _connector.GetColumnNameWithPrefix(columnName);
            string tableName = _connector.GetTableNameWithPrefix(sqlOperationEncryption.GetEncryptedTableName(deleteRecord.TableName));
            var typeAndId = _connector.GetColumnTypeAndId(valueColumn, tableName, _proxyDatabaseName);
            var queryBuilder = new QueryBuilder();
            var columnType = typeAndId.Item1;
            List<ISqlOperation> result = new List<ISqlOperation>();
            if (columnType != ColumnType.RangeColumn && columnType != ColumnType.NormalColumn)
            {
                result.Add(sqlOperationEncryption.EncryptDeleteRecordWithoutEncryptedIdentifier(deleteRecord, columnType));
            }
            else
            {
                var primaryKey = _connector.GetPrimaryKey(tableName, _proxyDatabaseName);
                var onion = new Onion();
                var detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(deleteRecord.Value), _masterKey, _iv, deleteRecord.TableName, deleteRecord.ColumnName);
                var bucketSizeAndIV = _connector.GetBucket(typeAndId.Item2, typeAndId.Item1, _proxyDatabaseName);
                int bucketSize = sqlOperationEncryption.GetBucketSize(bucketSizeAndIV.Item1, bucketSizeAndIV.Item2);
                var bucket = sqlOperationEncryption.GetBucket(bucketSize, detLayer);
                queryBuilder = new QueryBuilder();
                var query = queryBuilder.Select(primaryKey, tableName).Select(valueColumn, tableName).Select(_connector.GetIVColumnNameWithPrefix(columnName), tableName).Where(tableName, _connector.GetBucketColumnNameWithPrefix(columnName), bucket);
                var list = _connector.GetIdentifiersWithBucket(primaryKey, tableName, valueColumn, columnName, bucket, _proxyDatabaseName);
                List<string> identifiersToUpdate = new List<string>();
                
                foreach (Tuple<string, byte[], byte[]> value in list)
                {
                    detLayer = onion.DecryptRandomLayer(value.Item2, _masterKey, deleteRecord.TableName, deleteRecord.ColumnName, value.Item3);
                    var onionValue = onion.DecryptDeterministicLayer(detLayer, _masterKey, _iv, deleteRecord.TableName, deleteRecord.ColumnName);
                    var trueValue = Encoding.ASCII.GetString(onionValue);
                    if (trueValue == deleteRecord.Value)
                    {
                        identifiersToUpdate.Add(value.Item1);
                    }
                }
                foreach (string indentifier in identifiersToUpdate)
                {
                    var deleteRecordOperation = (DeleteRecordOperation)deleteRecord.Clone();
                    deleteRecordOperation.ColumnName = primaryKey;
                    deleteRecordOperation.TableName = tableName;
                    deleteRecordOperation.Value = APOSTROPHE + indentifier + APOSTROPHE;
                    result.Add(deleteRecordOperation);
                }
            }
            return result;
        }
        
        public Dictionary<string, List<Tuple<string, string>>> GetDatabaseStructure()
        {
            var encryption = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var result = new Dictionary<string, List<Tuple<string, string>>>();
            var encryptedStructure = _connector.GetStructure(_proxyDatabaseName);
            var iterator = encryptedStructure.GetEnumerator();
            while (iterator.MoveNext())
            {
                var value = iterator.Current;
                var tableName = encryption.DecryptTableName(value.Key);
                var encryptedColumns = value.Value;
                var list = new List<Tuple<string, string>>();
                foreach (Tuple<string, string> encryptedColumn in encryptedColumns)
                {
                    var column = encryption.DecryptColumName(tableName, encryptedColumn.Item1);
                    var columnAndType = new Tuple<string, string>(column, encryptedColumn.Item2);
                    list.Add(columnAndType);
                }
                result.Add(tableName, list);
            }
            return result;
        }
        public QueryAuxiliarData QueryOperation(QueryBuilder query)
        {
            var sqlOperationEncryption = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var queryAuxiliarData = new QueryAuxiliarData();
            var encryptedQueryBuilder = new QueryBuilder();
  
            var listWhereResults = new List<IResult>();

            for (int i = 0; i < query.SelectColumns.Count; i++)
            {
                var column = query.SelectColumns[i];
                
                //TODO: check if can remove results here
                queryAuxiliarData.Results.Add(new StringQueryResult());
                queryAuxiliarData.Results[i].ColumnName = column.Column; 
                queryAuxiliarData.Results[i].TableName = column.Table;

                var columnName = sqlOperationEncryption.GetEncryptedColumnName(column.Table, column.Column);
                var tableName = _connector.GetTableNameWithPrefix(sqlOperationEncryption.GetEncryptedTableName(column.Table));
                var mainColumnName = _connector.GetColumnNameWithPrefix(columnName);

                var typeAndId = _connector.GetColumnTypeAndId(mainColumnName, tableName, _proxyDatabaseName);

                encryptedQueryBuilder.Select(mainColumnName, tableName);

                if (typeAndId.Item1 == ColumnType.PrimaryColumn || typeAndId.Item1 == ColumnType.ForeignColumn)
                {
                    queryAuxiliarData.Values.Add(new GuidQueryResult(column.Table, column.Column, typeAndId.Item1));
                }
                else if (typeAndId.Item1 == ColumnType.UniqueColumn)
                {
                    queryAuxiliarData.Values.Add(new BinaryQueryResult(column.Table, column.Column, typeAndId.Item1));
                }
                else
                {
                    encryptedQueryBuilder.Select(_connector.GetIVColumnNameWithPrefix(columnName), tableName);
                    queryAuxiliarData.Values.Add(new BinaryQueryResult(column.Table, column.Column, typeAndId.Item1));
                    queryAuxiliarData.Values.Add(new BinaryQueryResult(column.Table, column.Column, typeAndId.Item1));
                }
            }
            
            encryptedQueryBuilder.JoinFields = GetEncryptedJoin(query.JoinFields, encryptedQueryBuilder);
            
            queryAuxiliarData.NumberOfNewSelects = EncryptWhereEquals(query, encryptedQueryBuilder, queryAuxiliarData);
            try
            {
                EncryptWhereRange(query, encryptedQueryBuilder, queryAuxiliarData);
            }
            catch (Exception e)
            {
                Console.WriteLine("Not a range column" + e);
                return null;
            }
            queryAuxiliarData.EncryptedQuery = encryptedQueryBuilder.Build();
            return queryAuxiliarData;
        }

        public List<StringQueryResult> DecryptQueryResult(int numberOfRows, QueryBuilder query, QueryAuxiliarData queryAuxiliarData)
        {            
            for (int i = 0; i < numberOfRows; i++)
            {
                //need to filter stuff before decrypt everything if it doesnt belong skip to next row
                var whereIsValid = IsValidRow(query, queryAuxiliarData.Values, queryAuxiliarData.WherePositions, i);
                if (whereIsValid)
                {
                    int k = 0;
                    var numberOfSelectFieldsToReturn = queryAuxiliarData.Values.Count - queryAuxiliarData.NumberOfNewSelects;

                    for (int j = 0; j < numberOfSelectFieldsToReturn; j++)
                    {
                        if (queryAuxiliarData.Values[j] is BinaryQueryResult)
                        {
                            string value = DecryptOnionValue(queryAuxiliarData.Values, i, j);
                            if (queryAuxiliarData.Values[j].Type != ColumnType.UniqueColumn)
                            {
                                j++; // need to sum 1 because ivColumns
                            }
                            queryAuxiliarData.Results[k++].AddSqlValue(value);
                        }
                        else
                        {
                            var valueCol = (GuidQueryResult) queryAuxiliarData.Values[j];
                            queryAuxiliarData.Results[k++].AddSqlValue(valueCol.Values[i]);
                        }
                    }
                }
            }
            return queryAuxiliarData.Results;

        }
        private IList<JoinField> GetEncryptedJoin(IList<JoinField> joinFields, QueryBuilder queryBuilder)
        {
            var sqlOperationEncryption = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var encryptedJoin = new List<JoinField>();
            foreach (JoinField joinField in joinFields)
            {
                var columnAlreadyJoinName = sqlOperationEncryption.GetEncryptedColumnName(joinField.TableAlreadyJoined, joinField.ColumnAlreadyJoined);
                var mainColumnAlreadyJoinName = _connector.GetColumnNameWithPrefix(columnAlreadyJoinName);
                var tableAlreadyJoinName = _connector.GetTableNameWithPrefix(sqlOperationEncryption.GetEncryptedTableName(joinField.TableAlreadyJoined));
                var columnToJoinName = sqlOperationEncryption.GetEncryptedColumnName(joinField.TableToJoin, joinField.ColumnToJoin);
                var mainColumnToJoinName = _connector.GetColumnNameWithPrefix(columnToJoinName);
                var tableToJoinName = _connector.GetTableNameWithPrefix(sqlOperationEncryption.GetEncryptedTableName(joinField.TableToJoin));
                queryBuilder.FromTables.Remove(tableToJoinName);
                encryptedJoin.Add(new JoinField()
                {
                    TableToJoin = tableToJoinName,
                    TableAlreadyJoined = tableAlreadyJoinName,
                    ColumnToJoin = mainColumnToJoinName,
                    ColumnAlreadyJoined = mainColumnAlreadyJoinName
                });
            }
            return encryptedJoin;
        }

        private void EncryptWhereRange(QueryBuilder query, QueryBuilder encryptedQueryBuilder, QueryAuxiliarData queryAuxiliarData)
        {
            var sqlOperationEncryption = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var whereFieldsTypes = new Dictionary<int, IList<WhereField>> () { 
                { WHEREHIGHER, query.WhereHigherThanFields}, { WHERELESS , query.WhereLessThanFields}, 
                { WHEREHIGHEROREQUAL, query.WhereHigherOrEqualFields}, { WHERELESSOREQUAL, query.WhereLessOrEqualFields }};

            foreach(var keyValuePair in whereFieldsTypes){
                var whereType = keyValuePair.Key;
                var whereFields = keyValuePair.Value;

                for (int i = 0; i < whereFields.Count; i++)
                {
                    WhereField where = whereFields[i];
                    var bucketColum = _connector.GetBucketColumnNameWithPrefix(sqlOperationEncryption.GetEncryptedColumnName(where.Table, where.Column));
                    var columnName = sqlOperationEncryption.GetEncryptedColumnName(where.Table, where.Column);
                    var mainColumnName = _connector.GetColumnNameWithPrefix(columnName);
                    var tableName = _connector.GetTableNameWithPrefix(sqlOperationEncryption.GetEncryptedTableName(where.Table));
                    var typeAndId = _connector.GetColumnTypeAndId(mainColumnName, tableName, _proxyDatabaseName);
                    if (typeAndId.Item1 != ColumnType.RangeColumn)
                    {
                        throw new Exception("Column is not RangeColumn");
                    }
                    var bucketSizeAndIV = _connector.GetBucket(typeAndId.Item2, typeAndId.Item1, _proxyDatabaseName);
                    int bucketSize = sqlOperationEncryption.GetBucketSize(bucketSizeAndIV.Item1, bucketSizeAndIV.Item2);
                    var MaxRangeAndCanBeNegative = _connector.GetBucketMaxRange(typeAndId.Item2, _proxyDatabaseName);
                    int maxRange = sqlOperationEncryption.GetMaxRange(MaxRangeAndCanBeNegative.Item1, MaxRangeAndCanBeNegative.Item2);
                    List<string> bucketLimit = null;
                    if (whereType == WHERELESS || whereType == WHERELESSOREQUAL)
                    {
                        bucketLimit = sqlOperationEncryption.GetBucketsLessThan(bucketSize, maxRange, where.Value, where.Table, where.Column, MaxRangeAndCanBeNegative.Item3);
                    }
                    else if (whereType == WHEREHIGHER || whereType == WHEREHIGHEROREQUAL)
                    {
                        bucketLimit = sqlOperationEncryption.GetBucketsHigherThan(bucketSize, maxRange, where.Value, where.Table, where.Column, MaxRangeAndCanBeNegative.Item3);
                    }
                    foreach (string bucket in bucketLimit)
                    {
                        encryptedQueryBuilder.WhereEqualOr(tableName, _connector.GetBucketColumnNameWithPrefix(columnName), bucket);
                    }
                    CheckIfWhereIsInSelectIfNotAddIt(encryptedQueryBuilder, queryAuxiliarData, where, columnName, mainColumnName, tableName, bucketSize, whereType, i);
                }
            }

        }

        private void EncryptWhereHigherThan(QueryBuilder query, QueryBuilder queryBuilder, QueryAuxiliarData queryAuxiliarData, ref int numberOfNewSelects)
        {
            var sqlOperationEncryption = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var onion = new Onion();
            for (int i = 0; i < query.WhereHigherThanFields.Count; i++)
            {
                var where = query.WhereHigherThanFields[i];
                var bucketColum = _connector.GetBucketColumnNameWithPrefix(sqlOperationEncryption.GetEncryptedColumnName(where.Table, where.Column));
                var columnName = sqlOperationEncryption.GetEncryptedColumnName(where.Table, where.Column);
                var mainColumnName = _connector.GetColumnNameWithPrefix(columnName);
                var tableName = _connector.GetTableNameWithPrefix(sqlOperationEncryption.GetEncryptedTableName(where.Table));
                var typeAndId = _connector.GetColumnTypeAndId(mainColumnName, tableName, _proxyDatabaseName);
                var MaxRangeAndCanBeNegative = _connector.GetBucketMaxRange(typeAndId.Item2, _proxyDatabaseName);
                if (typeAndId.Item1 != ColumnType.RangeColumn)
                {
                    throw new Exception("Column is not RangeColumn");
                }
                var bucketSizeAndIV = _connector.GetBucket(typeAndId.Item2, typeAndId.Item1, _proxyDatabaseName);
                int bucketSize = sqlOperationEncryption.GetBucketSize(bucketSizeAndIV.Item1, bucketSizeAndIV.Item2);
                int maxRange = sqlOperationEncryption.GetMaxRange(MaxRangeAndCanBeNegative.Item1, MaxRangeAndCanBeNegative.Item2);
                var bucketLimit = sqlOperationEncryption.GetBucketsHigherThan(bucketSize, maxRange, where.Value, where.Table, where.Column, MaxRangeAndCanBeNegative.Item3);
                foreach (string bucket in bucketLimit)
                {
                    queryBuilder.WhereEqualOr(tableName, _connector.GetBucketColumnNameWithPrefix(columnName), bucket);
                }
                CheckIfWhereIsInSelectIfNotAddIt(queryBuilder, queryAuxiliarData, where, columnName, mainColumnName, tableName, bucketSize, WHEREHIGHER, i);
            }

        }

        private int EncryptWhereEquals(QueryBuilder query, QueryBuilder queryBuilder, QueryAuxiliarData queryAuxiliarData)
        {
            int numberOfNewSelects = 0;
            var sqlOperationEncryption = new EncryptSqlOperation(_masterKey, _iv, _proxyDatabaseName, _connector);
            var onion = new Onion();
            for (int i = 0; i < query.WhereEqualFields.Count; i++)
            {
                var where = query.WhereEqualFields[i];
                var bucketColumn = _connector.GetBucketColumnNameWithPrefix(sqlOperationEncryption.GetEncryptedColumnName(where.Table, where.Column));
                var columnName = sqlOperationEncryption.GetEncryptedColumnName(where.Table, where.Column);
                var mainColumnName = _connector.GetColumnNameWithPrefix(columnName);
                var tableName = _connector.GetTableNameWithPrefix(sqlOperationEncryption.GetEncryptedTableName(where.Table));
                var typeAndId = _connector.GetColumnTypeAndId(mainColumnName, tableName, _proxyDatabaseName);

                var columnType = typeAndId.Item1;
                byte[] bucket;
                if (columnType == ColumnType.PrimaryColumn || columnType == ColumnType.ForeignColumn)
                {
                    var isGuid = Guid.TryParse(where.Value, out Guid guid);
                    if (isGuid)
                    {
                        queryBuilder.Where(tableName, mainColumnName, where.Value);
                    }
                    else
                    {
                        throw new Exception("Primary key or Foreign key must be a GUID");
                    }
                    continue;
                }
                else if (columnType == ColumnType.UniqueColumn)
                {
                    var detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(where.Value), _masterKey, _iv, where.Table, where.Column);
                    queryBuilder.Where(tableName, mainColumnName, detLayer);
                }
                var bucketSizeAndIV = _connector.GetBucket(typeAndId.Item2, typeAndId.Item1, _proxyDatabaseName);
                int bucketSize = sqlOperationEncryption.GetBucketSize(bucketSizeAndIV.Item1, bucketSizeAndIV.Item2);
                if (columnType == ColumnType.RangeColumn)
                {
                    var maxRangeAndCanBeNegative = _connector.GetBucketMaxRange(typeAndId.Item2, _proxyDatabaseName);
                    int maxRange = sqlOperationEncryption.GetMaxRange(maxRangeAndCanBeNegative.Item1, maxRangeAndCanBeNegative.Item2);
                    bucket = sqlOperationEncryption.GetRangeBucket(bucketSize, maxRange, where.Value, where.Table, where.Column);
                }
                else
                {
                    var detLayer = onion.EncryptDeterministicLayer(Encoding.ASCII.GetBytes(where.Value), _masterKey, _iv, where.Table, where.Column);
                    bucket = sqlOperationEncryption.GetBucket(bucketSize, detLayer);
                }
                queryBuilder.Where(tableName, _connector.GetBucketColumnNameWithPrefix(columnName), bucket);

                CheckIfWhereIsInSelectIfNotAddIt(queryBuilder, queryAuxiliarData, where, columnName, mainColumnName, tableName, bucketSize, WHEREEQUALS, i);
            }

            return numberOfNewSelects;
        }
        private void CheckIfWhereIsInSelectIfNotAddIt(QueryBuilder queryBuilder, QueryAuxiliarData queryAuxiliarData,
            WhereField where, string columnName, string mainColumnName, string tableName, int bucketSize, int type, int indexOnWhere)
        {
            bool found = false;
            for (int j = 0; j < queryAuxiliarData.Values.Count; j++)
            {
                if (queryAuxiliarData.Values[j].ColumnName == where.Column && queryAuxiliarData.Values[j].TableName == where.Table)
                {
                    queryAuxiliarData.WherePositions.Add(new Tuple<int, int, int>(j, type, indexOnWhere));
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                queryBuilder.Select(mainColumnName, tableName);
                queryBuilder.Select(_connector.GetIVColumnNameWithPrefix(columnName), tableName);
                queryAuxiliarData.WherePositions.Add(new Tuple<int, int, int>(queryAuxiliarData.Values.Count, type, indexOnWhere));
                queryAuxiliarData.Values.Add(new BinaryQueryResult(where.Table, where.Column));
                queryAuxiliarData.Values.Add(new BinaryQueryResult(where.Table, where.Column));
                queryAuxiliarData.NumberOfNewSelects += 2;
            }
        }

        private bool IsValidRow(QueryBuilder query, List<IResult> values, List<Tuple<int, int, int>> wherePositions, int i)
        {
            for (int j = 0; j < wherePositions.Count; j++)
            {
                var pos = wherePositions[j];
                if (pos.Item2 == WHEREEQUALS)
                {
                    if (!CheckIfWhereEqualIsValid(query.WhereEqualFields, values, i, j, pos))
                    {
                        return false;
                    }
                }
                else if (pos.Item2 == WHEREHIGHER)
                {
                    if (!CheckIfWhereHigherThanIsValid(query.WhereHigherThanFields, values, i, j, pos))
                    {
                        return false;
                    }
                }
                else if (pos.Item2 == WHERELESS)
                {
                    if (!CheckIfWhereLessThanIsValid(query.WhereLessThanFields, values, i, j, pos))
                    {
                        return false;
                    }
                }
                else if (pos.Item2 == WHEREHIGHEROREQUAL)
                {
                    if (!CheckIfWhereHigherOrEqualIsValid(query.WhereHigherOrEqualFields, values, i, j, pos))
                    {
                        return false;
                    }
                }
                else if (pos.Item2 == WHERELESSOREQUAL)
                {
                    if (!CheckIfWhereLessOrEqualIsValid(query.WhereLessOrEqualFields, values, i, j, pos))
                    {
                        return false;
                    }
                }
                
            }
            return true;

        }
        private bool CheckIfWhereLessOrEqualIsValid(IList<WhereField> whereLessThanFields, List<IResult> values, int i, int j, Tuple<int, int, int> pos)
        {
            if (values[pos.Item1] is BinaryQueryResult)
            {
                string value = DecryptOnionValue(values, i, pos.Item1);
                if (int.Parse(whereLessThanFields[pos.Item3].Value) < int.Parse(value))
                {
                    return false;
                }
                j++;
            }
            else if (values[pos.Item1] is GuidQueryResult guidQueryResult)
            {
                if (int.Parse(guidQueryResult.Values[i]) > int.Parse(whereLessThanFields[pos.Item3].Value))
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckIfWhereHigherOrEqualIsValid(IList<WhereField> whereHigherThanFields, List<IResult> values, int i, int j, Tuple<int, int, int> pos)
        {
            if (values[pos.Item1] is BinaryQueryResult)
            {
                string value = DecryptOnionValue(values, i, pos.Item1);
                if (int.Parse(whereHigherThanFields[pos.Item3].Value) > int.Parse(value))
                {
                    return false;
                }
                j++;
            }
            else if (values[pos.Item1] is GuidQueryResult guidQueryResult)
            {
                if (int.Parse(guidQueryResult.Values[i]) < int.Parse(whereHigherThanFields[pos.Item3].Value))
                {
                    return false;
                }
            }
            return true;
        }
        private bool CheckIfWhereLessThanIsValid(IList<WhereField> whereLessThanFields, List<IResult> values, int i, int j, Tuple<int, int, int> pos)
        {
            if (values[pos.Item1] is BinaryQueryResult)
            {
                string value = DecryptOnionValue(values, i, pos.Item1);
                if (int.Parse(whereLessThanFields[pos.Item3].Value) <= int.Parse(value))
                {
                    return false;
                }
                j++;
            }
            else if (values[pos.Item1] is GuidQueryResult guidQueryResult)
            {
                if (int.Parse(guidQueryResult.Values[i]) >= int.Parse(whereLessThanFields[pos.Item3].Value))
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckIfWhereHigherThanIsValid(IList<WhereField> whereHigherThanFields, List<IResult> values, int i, int j, Tuple<int, int, int> pos)
        {
            if (values[pos.Item1] is BinaryQueryResult)
            {
                string value = DecryptOnionValue(values, i, pos.Item1);
                if (int.Parse(whereHigherThanFields[pos.Item3].Value) >= int.Parse(value))
                {
                    return false;
                }
                j++;
            }
            else if (values[pos.Item1] is GuidQueryResult guidQueryResult)
            {
                if (int.Parse(guidQueryResult.Values[i]) <= int.Parse(whereHigherThanFields[pos.Item3].Value))
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckIfWhereEqualIsValid(IList<WhereField> fields, List<IResult> values, int i, int j, Tuple<int, int, int> pos)
        {
            if (values[pos.Item1] is BinaryQueryResult)
            {
                string value = DecryptOnionValue(values, i, pos.Item1);
                if (!fields[pos.Item3].Value.Equals(value))
                {
                    return false;
                }
                j++;
            }
            else if (values[pos.Item1] is GuidQueryResult guidQueryResult)
            {
                if (guidQueryResult.Values[i] != fields[pos.Item3].Value)
                {
                    return false;
                }
            }
            return true;
        }

        private string DecryptOnionValue(List<IResult> values, int i, int j)
        {
            var onion = new Onion();
            var valueCol = (BinaryQueryResult)values[j];
            if (valueCol.Values[i] == null)
                return null;
            byte[] valueInBytes;
            if (values[j].Type != ColumnType.UniqueColumn)
            {
                var IVCol = (BinaryQueryResult)values[++j];

                var detLayer = onion.DecryptRandomLayer(valueCol.Values[i], _masterKey, valueCol.TableName, valueCol.ColumnName, IVCol.Values[i]);
                valueInBytes = onion.DecryptDeterministicLayer(detLayer, _masterKey, _iv, valueCol.TableName, valueCol.ColumnName);
            }
            else
            {
                valueInBytes = onion.DecryptDeterministicLayer(valueCol.Values[i], _masterKey, _iv, valueCol.TableName, valueCol.ColumnName);
            }
            var value = Encoding.ASCII.GetString(valueInBytes);
            return value;
        }
    }
}