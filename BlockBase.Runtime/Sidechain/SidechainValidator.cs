using System;
using System.Linq;
using System.Threading.Tasks;
using BlockBase.DataPersistence.ProducerData;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Sidechain;
using Google.Protobuf;

namespace BlockBase.Runtime.Sidechain
{
    public class SidechainValidator
    {
        private IMainchainService _mainChainService;
        private IMongoDbProducerService _mongoDbProducerService;
        private SidechainPool _sidechainPool;
        public SidechainValidator(IMainchainService mainchainService, IMongoDbProducerService mongoDbProducerService, SidechainPool sidechainPool)
        {
            _mainChainService = mainchainService;
            _mongoDbProducerService = mongoDbProducerService;
            _sidechainPool = sidechainPool;
        }
        public async Task<byte[]> GetValidationAnswer()
        {
            var lastValidBlockheader = (await _mainChainService.GetLastValidSubmittedBlockheader(_sidechainPool.ClientAccountName, (int)_sidechainPool.BlocksBetweenSettlement)).ConvertToBlockHeader(); 
            var blockHashNumber = BitConverter.ToInt32(lastValidBlockheader.BlockHash);
            var chosenBlockSequenceNumber = ((ulong) blockHashNumber % lastValidBlockheader.SequenceNumber) + 1;
            var chosenBlock = (await _mongoDbProducerService.GetSidechainBlocksSinceSequenceNumberAsync(_sidechainPool.ClientAccountName, chosenBlockSequenceNumber, chosenBlockSequenceNumber)).SingleOrDefault();
            var blockBytes = chosenBlock.ConvertToProto().ToByteArray();
            return null;
        }
    }
}