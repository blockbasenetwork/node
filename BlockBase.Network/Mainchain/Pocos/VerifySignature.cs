using Newtonsoft.Json;
using BlockBase.Domain.Eos;
using EosSharp.Core.Api.v1;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class VerifySignature
    {
        public string Account { get; set; }

        public string BlockHash { get; set; }

        public string Signature { get; set; }

        public Transaction Transaction { get; set; }

        public byte[] PackedTransaction { get; set; }
    }
}