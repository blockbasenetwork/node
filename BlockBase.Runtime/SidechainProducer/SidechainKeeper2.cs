using BlockBase.Network.Sidechain;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Runtime.SidechainProducer
{
    public class SidechainKeeper2
    {
        private ConcurrentDictionary<string, SidechainContext> Sidechains { get; }

        public SidechainKeeper2()
        {
            Sidechains = new ConcurrentDictionary<string, SidechainContext>();
        }

        public IEnumerable<SidechainContext> GetSidechains()
        {
            return Sidechains.Values.ToList();
        }

        public bool ContainsKey(string sidechainName)
        {
            return Sidechains.ContainsKey(sidechainName);
        }

        public bool TryGet(string sidechainName, out SidechainContext sidechainContext)
        {
            return Sidechains.TryGetValue(sidechainName, out sidechainContext);
        }

        public bool TryAddSidechain(SidechainContext sidechainContext)
        {
            return Sidechains.TryAdd(sidechainContext.SidechainPool.ClientAccountName, sidechainContext);
        }

        public bool TryRemoveSidechain(string sidechainName, out SidechainContext sidechainContext)
        {
            return Sidechains.TryRemove(sidechainName, out sidechainContext);
        }
    }
}