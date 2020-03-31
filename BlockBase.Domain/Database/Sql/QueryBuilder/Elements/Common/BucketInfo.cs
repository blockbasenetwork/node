using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class BucketInfo
    {
        public int? EqualityNumberOfBuckets { get; set; }
        public int? RangeNumberOfBuckets { get; set; }
        public int? BucketMinRange { get; set; }
        public int? BucketMaxRange { get; set; }

        public BucketInfo Clone()
        {
            return new BucketInfo()
            {
                EqualityNumberOfBuckets = EqualityNumberOfBuckets,
                RangeNumberOfBuckets = RangeNumberOfBuckets,
                BucketMinRange = BucketMinRange,
                BucketMaxRange = BucketMaxRange
            };

        }
    }
}
