using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class DataType
    {
        public DataTypeEnum DataTypeName { get; set; }

        public BucketInfo BucketInfo { get; set; }

        public DataType Clone()
        {
            return new DataType()
            {
                DataTypeName = DataTypeName,
                BucketInfo  = BucketInfo.Clone()
            };
        }
    }
}
