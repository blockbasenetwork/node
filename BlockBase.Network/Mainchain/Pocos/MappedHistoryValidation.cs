
using System.Collections.Generic;
using EosSharp.Core.Api.v1;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class MappedHistoryValidation
    {
        public string Account { get; set; }
       
        public string BlockHash { get; set; }

        public List<string> VerifySignatures {get; set;}
        
        public List<string> SignedProducers {get; set;}

        public string BlockByteInHexadecimal { get; set; }

        public Transaction Transaction { get; set; }

        public byte[] PackedTransaction { get; set; }
    }
}