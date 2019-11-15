using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
namespace BlockBase.Network.Mainchain.Pocos
{
    public class Sidechains
    {
        public string Name { get; set; }
        public string State { get; set; }
        public bool IsProductionTime { get; set; }
        public uint NeededProducers{ get; set; }
        public uint ActualProducers { get; set; }
        public string Reward { get; set; }
        public string BlockHeader{ get; set; }
        public uint TotalBlocks { get; set; }
        public DateTime CreationDate{ get; set; }
        public Sidechains(){
        }
    }
    
}