using BlockBase.Network.Sidechain;
using System.Collections.Concurrent;

namespace BlockBase.Runtime.SidechainProducer
{
    public class SidechainKeeper
    {
        public ConcurrentDictionary<string, SidechainPool> Sidechains { get; }

        public SidechainKeeper()
        {
            Sidechains = new ConcurrentDictionary<string, SidechainPool>();
        }

        public bool TryAddSidechain(SidechainPool sidechain)
        {
            return Sidechains.TryAdd(sidechain.SidechainName, sidechain);
        }

        public bool TryRemoveSidechain(SidechainPool sidechain)
        {
            return Sidechains.TryRemove(sidechain.SidechainName, out sidechain);
        }
    }
}