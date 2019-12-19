using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class DataType
    {
        public DataTypeEnum DataTypeName { get; set; }

        public BucketInfo BucketInfo { get; set; }

        public DataType()
        {
            BucketInfo = new BucketInfo();
        }

        public DataType(DataTypeEnum dataTypeEnum)
        {
            DataTypeName = dataTypeEnum;
        }

        public DataType(DataTypeEnum dataTypeEnum, BucketInfo bucketInfo)
        {
            DataTypeName = dataTypeEnum;
            BucketInfo = bucketInfo;
        }

        public DataType Clone()
        {
            return new DataType()
            {
                DataTypeName = DataTypeName,
                BucketInfo = BucketInfo.Clone()
            };
        }
    }
}
