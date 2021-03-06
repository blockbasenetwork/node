﻿using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BlockBase.Domain.Database.Info
{
    public class InfoRecord
    {
        public string Name { get; set; }
        public string KeyName { get; set; }
        public string KeyManage { get; set; }
        public string ParentIV { get; set; }
        public string IV { get; set; }
        public string Data { get; set; }
        public string LocalNameHash { get; set; }
        public LocalData LData { get; set; }

        public InfoRecord()
        {
        }

        public InfoRecord(string json)
        {
            JObject jObject = JObject.Parse(json);
            Name = (string)jObject[InfoTableConstants.NAME];
            KeyName = (string)jObject[InfoTableConstants.KEY_NAME];
            KeyManage = (string)jObject[InfoTableConstants.KEY_MANAGE];
            ParentIV = (string)jObject[InfoTableConstants.PARENT];
            IV = (string)jObject[InfoTableConstants.IV];
            Data = (string)jObject[InfoTableConstants.DATA];
        }

        public InfoRecord Clone()
        {
            return new InfoRecord()
            {
                Name = Name,
                KeyName = KeyName,
                KeyManage = KeyManage,
                LData = LData,
                Data = Data,
                ParentIV = ParentIV,
                IV = IV,
                LocalNameHash = LocalNameHash
            };
        }

        public class LocalData
        {
            public DataType DataType { get; set; }
            public IList<ColumnConstraint> ColumnConstraints { get; set; }
            public string EncryptedEqualityColumnName { get; set; }
            public string EncryptedRangeColumnName { get; set; }
            public string EncryptedIVColumnName { get; set; }
            public LocalData()
            {
            }
            public LocalData(string encryptedEqualityColumnName, string encryptedRangeColumnName, string encryptedIVColumnName)
            {
                EncryptedEqualityColumnName = encryptedEqualityColumnName;
                EncryptedRangeColumnName = encryptedRangeColumnName;
                EncryptedIVColumnName = encryptedIVColumnName;
            }
        }
    }
}
