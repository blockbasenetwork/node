using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class DataType
    {
        public DataTypeEnum DataTypeName { get; set; }

        public int? BucketSize { get; set; }
        public int? BucketMinRange { get; set; }
        public int? BucketMaxRange { get; set; }

        public DataType Clone()
        {
            return new DataType()
            {
                DataTypeName = DataTypeName,
                BucketMaxRange = BucketMaxRange,
                BucketMinRange = BucketMinRange,
                BucketSize = BucketSize
            };
        }
    }
}
