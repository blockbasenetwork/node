using BlockBase.Domain.Eos;
using Newtonsoft.Json;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class BlockheaderTransaction
    {
        [JsonProperty(EosParameterNames.NAME)]
        public string Name { get; set; }

        [JsonProperty(EosParameterNames.BLOCK)]
        public BlockheaderTable Block { get; set; }

        public BlockheaderTransaction(string name , BlockheaderTable block)
        {
            Name = name;
            Block = block;
        }
    }
}
