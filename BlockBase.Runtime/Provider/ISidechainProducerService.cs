using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Provider;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Runtime.Provider
{
    public interface ISidechainProducerService
    {
        Task AddSidechainToProducerAndStartIt(string sidechainName, int producerType, bool automatic = false);
        void RemoveSidechainFromProducerAndStopIt(string sidechainName);
        Task LoadAndRunSidechainsFromRecoverDB();
        Task Run(bool recoverChains = true);

        bool DoesChainExist(string sidechainName);

        SidechainContext GetSidechainContext(string sidechainName);
        IEnumerable<SidechainContext> GetSidechainContexts();
    }
}
