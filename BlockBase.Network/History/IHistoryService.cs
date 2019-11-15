using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Network.Mainchain.Pocos;
using System;
using BlockBase.Network.History.Pocos;

namespace BlockBase.Network.History
{
    public interface IHistoryService
    {
        Task<List<Sidechains>> GetSidechainList();
        Task<List<Producers>> GetProducerList();
        Task<List<ProducerDetail>> GetProducerDetail(string accountName);
        Task<SidechainDetail> GetSidechainDetail(string chainName);
        Task<List<SidechainBlockHeader>> GetBlockHeaderList(string chainName);
        Task<List<SidechainBlackList>> GetBlackLists(string chainName);
        Task<List<SidechainProducer>> GetSidechainProducerList(string chainName);
    }
}