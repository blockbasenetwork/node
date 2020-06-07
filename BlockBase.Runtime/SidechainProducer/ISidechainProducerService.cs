// using BlockBase.Network.Sidechain;
// using BlockBase.Runtime.Sidechain;
// using BlockBase.Network.Mainchain.Pocos;
// using BlockBase.Utils;
// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Text;
// using System.Threading.Tasks;

// namespace BlockBase.Runtime.SidechainProducer
// {
//     public interface ISidechainProducerService
//     {
//         void AddSidechainToProducerAndStartIt(SidechainPool sidechain);
//         void RemoveSidechainFromProducerAndStopIt(SidechainPool sidechain);
//         Task GetSidechainsFromRecoverDB();
//         Task Run(bool RecoverChains = true);
//         Dictionary<string, SidechainPool> GetSidechains();
//     }
// }
