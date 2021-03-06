﻿using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class CandidateTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }

        [JsonProperty(EosAtributeNames.PUBLIC_KEY)]
        public string PublicKey { get; set; }

        [JsonProperty(EosAtributeNames.SECRET_HASH)]
        public string SecretHash { get; set; }

        [JsonProperty(EosAtributeNames.SECRET)]
        public string Secret { get; set; }

        [JsonProperty(EosAtributeNames.WORK_DURATION_IN_SECONDS)]
        public ulong WorkTimeInSeconds { get; set; }

        [JsonProperty(EosAtributeNames.PRODUCER_TYPE)]
        public uint ProducerType { get; set; }
    }    
}
