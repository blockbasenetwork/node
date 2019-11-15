using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    public class MongoDBConfigurations
    {
        public string MongoDBAddress { get; set; }
        public string MongoDBDatabase { get; set; }
        public string MongoDBActionTableName { get; set; }
    }
}
