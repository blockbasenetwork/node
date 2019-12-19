using Newtonsoft.Json.Linq;

namespace BlockBase.Domain.Database.Info
{
    public class InfoRecord
    {
        public string Name { get; set; }
        public string KeyName { get; set; }
        public string KeyManage { get; set; }
        public string ParentIV { get; set; }
        public string IV { get; set; }
        public string LocalNameHash { get; set; }
        public string Data { get; set; }
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

        public class LocalData
        {
            public string EncryptedEqualityColumnName { get; set; }
            public string EncryptedRangeColumnName { get; set; }
            public string EncryptedIVColumnName { get; set; }
        }
    }
}