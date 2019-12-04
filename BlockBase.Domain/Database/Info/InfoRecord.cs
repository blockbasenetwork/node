using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Info
{
    public class InfoRecord
    {

        public string Name { get; set; }
        public string KeyRead { get; set; }
        public string KeyManage { get; set; }
        public string Parent { get; set; }
        public string IV { get; set; }
        public bool? IsDataEncrypted { get; set; }
        public IList<InfoRecord> Children { get; set; }


        public InfoRecord()
        {
            Children = new List<InfoRecord>();
        }

        public InfoRecord(string json) : this()
        {
            JObject jObject = JObject.Parse(json);
            Name = (string)jObject[InfoTableConstants.NAME];
            KeyRead = (string)jObject[InfoTableConstants.KEY_READ];
            KeyManage = (string)jObject[InfoTableConstants.KEY_MANAGE];
            Parent = (string)jObject[InfoTableConstants.PARENT];
            IV = (string)jObject[InfoTableConstants.IV];
            IsDataEncrypted = (bool?)jObject[InfoTableConstants.DATA_ENCRYPTED];
        }
    }
}
