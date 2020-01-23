﻿using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class ClientTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }
        [JsonProperty(EosAtributeNames.PUBLIC_KEY)]
        public string PublicKey { get; set; }
    }
}
