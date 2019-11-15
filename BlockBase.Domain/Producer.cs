using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BlockBase.Domain
{
    public class Producer
    {
        public IPEndPoint Address { get; set; }
        public RSAParameters PublicKey { get; set; }

        //TODO Verify again the utility, if we use it create pool classe
        //public IList<Pool> CurrentPools { get; set; }

        public Producer() { }

        public Producer(IPEndPoint address, RSAParameters privateKey, RSAParameters publicKey)
        {
            Address = address;
            PublicKey = publicKey;
        }
    }
}
