using Newtonsoft.Json;
using BlockBase.Domain.Eos;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class BlockCountTable
    {
        [JsonProperty(EosAtributeNames.KEY)]
        public string Key { get; set; }

        [JsonProperty(EosAtributeNames.BLOCKS_FAILED)]
        public uint blocksfailed { get; set; }

        [JsonProperty(EosAtributeNames.BLOCKS_PRODUCED)]
        public uint blocksproduced { get; set; }
    }
}