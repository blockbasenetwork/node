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
        public bool IsNameEncrypted { get; set; }
    }
}
