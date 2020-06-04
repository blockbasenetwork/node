using BlockBase.Network.Sidechain;
using BlockBase.Runtime.Sidechain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Runtime.SidechainProducer
{
    public interface ISidechainProducerService2
    {
        Task AddSidechainToProducerAndStartIt(string sidechainName, int producerType);
        void RemoveSidechainFromProducerAndStopIt(string sidechainName);
        Task LoadAndRunSidechainsFromRecoverDB();
        Task Run(bool recoverChains = true);

        bool DoesChainExist(string sidechainName);

        SidechainContext GetSidechainContext(string sidechainName);
        IEnumerable<SidechainContext> GetSidechainContexts();
    }
}
