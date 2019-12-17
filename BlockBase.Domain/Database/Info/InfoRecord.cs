﻿using Newtonsoft.Json.Linq;

namespace BlockBase.Domain.Database.Info
{
    public class InfoRecord
    {
        public string Name { get; set; }
        public string KeyName { get; set; }
        public string KeyManage { get; set; }
        public string ParentIV { get; set; }
        public string IV { get; set; }
        public bool? IsDataEncrypted { get; set; }
        public string LocalNameHash { get; set; }
        public string Data { get; set; }

        public InfoRecord()
        {
        }

        public InfoRecord(string json)
        {
            JObject jObject = JObject.Parse(json);
            Name = (string)jObject[InfoTableConstants.NAME];
            KeyName = (string)jObject[InfoTableConstants.KEY_READ];
            KeyManage = (string)jObject[InfoTableConstants.KEY_MANAGE];
            ParentIV = (string)jObject[InfoTableConstants.PARENT];
            IV = (string)jObject[InfoTableConstants.IV];
            IsDataEncrypted = (bool?)jObject[InfoTableConstants.IS_DATA_ENCRYPTED];
            Data = (string)jObject[InfoTableConstants.DATA];
        }
    }
}