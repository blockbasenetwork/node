using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Constants
{
    public class DatabaseConstants
    {
        public virtual string METAINFO_TABLE_NAME => "MetaInfo";
        public virtual string COLUMN_TYPE_TABLE_NAME => "ColumnType";
        public virtual string TYPE_COLUMN_NAME => "Type";
        public virtual string ID => "Id";
        public virtual  string TABLE_NAME_PREFIX => "Table_";
        public virtual string MAIN_ONION_PREFIX => "Value_";
        public virtual string IV_NAME_PREFIX => "IV_";
        public virtual string BUCKET_COLUMN_PREFIX => "Bucket_";
        public virtual string COLUMN_NAME => "ColumnName";
        public virtual string COLUMN_INFO_TABLE_NAMES => "TableName";
        public virtual string BUCKETINFO_TABLE => "Bucketinfo";
        public virtual string NUMBER_OF_BUCKETS_COLUMN => "NumberOfBuckets";
        public virtual string MAXRANGE_COLUMN => "MaxRange";
        public virtual string COLUMN_INFO_TABLE => "ColumnInfo";
        public virtual string MAX_RANGE_IV_COLUMN => "MaxRangeIV";
        public virtual string BUCKET_SIZE_COLUMN_NAME => "BucketSize";
        public virtual string BUCKET_SIZE_IV_COLUMN_NAME => "BucketSizeIV";
        public virtual string CANBENEGATIVE_COLUMN => "CanBeNegative";
        public virtual string COLUMN_ID_NAME => "ColumnId";
        public virtual string COLUMN_TYPE_ID => "ColumnTypeId";
        public virtual string PRIMARY_COLUMNS_TABLE_NAME => "PrimaryColumns";
        public virtual string FOREIGN_COLUMNS_TABLE_NAME => "ForeignColumns";
        public virtual string UNIQUE_COLUMNS_TABLE_NAME => "UniqueColumns";
        public virtual string NORMAL_COLUMNS_TABLE_NAME => "NormalColumns";
        public virtual string RANGE_COLUMNS_TABLE_NAME => "RangeColumns";
        public virtual string PRIMARY_COLUMNS_TABLE_NAME_ID => "1";
        public virtual string FOREIGN_COLUMNS_TABLE_NAME_ID => "2";
        public virtual string UNIQUE_COLUMNS_TABLE_NAME_ID => "3";
        public virtual string RANGE_COLUMNS_TABLE_NAME_ID => "4";
        public virtual string NORMAL_COLUMNS_TABLE_NAME_ID => "5";
        public virtual string LAST_BLOCK => "LastBlockRead";
    }
}
