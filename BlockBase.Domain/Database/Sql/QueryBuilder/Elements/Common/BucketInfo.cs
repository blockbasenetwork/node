using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class BucketInfo
    {
        public int? EqualityBucketSize { get; set; }
        public int? RangeBucketSize { get; set; }
        public int? BucketMinRange { get; set; }
        public int? BucketMaxRange { get; set; }

        public BucketInfo Clone()
        {
            return new BucketInfo()
            {
                EqualityBucketSize = EqualityBucketSize,
                RangeBucketSize = RangeBucketSize,
                BucketMinRange = BucketMinRange,
                BucketMaxRange = BucketMaxRange
            };

        }
    }
}
