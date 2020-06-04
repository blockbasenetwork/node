using System.ComponentModel; 
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BlockBase.Domain.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]        
    public enum NetworkType
    {
        [Description("All")]
        All,
        [Description("Mainnet")]
        Mainnet,
        [Description("Jungle")]
        Jungle
    }
}