using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlockBase.Domain.Protos;
using BlockBase.Utils.Crypto;

namespace BlockBase.Domain.Blockchain
{
    public class Block : ICloneable
    {
        public BlockHeader BlockHeader { get; set; }
        public IList<Transaction> Transactions { get; set; }

        public Block(BlockHeader blockHeader, IList<Transaction> transactionsList)
        {
            BlockHeader = blockHeader;
            Transactions = transactionsList;
        }
        public Block()
        {
            BlockHeader = new BlockHeader();
            Transactions = new List<Transaction>();
        }

        public void CalculateMerkleRoot()
        {   
            var merkleLeaves = new List<byte[]>();

            foreach (var transaction in Transactions){
                merkleLeaves.Add(HashHelper.Sha256Data(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(transaction))));
            }

            var hash = MerkleTreeHelper.CalculateMerkleRootHash(merkleLeaves);
            BlockHeader.MerkleRoot = hash;
        }

         public BlockProto ConvertToProto()
        {
            var blockProto = new BlockProto()
            {
                BlockHeader = BlockHeader.ConvertToProto(),
                Transactions = { Transactions.Select(t => t.ConvertToProto()) }
            };
            return blockProto;
        }

        public Block SetValuesFromProto(BlockProto blockProto)
        {
            var block = new Block()
            {
                BlockHeader = new BlockHeader().SetValuesFromProto(blockProto.BlockHeader),
                Transactions = blockProto.Transactions.Select(t => new Transaction().SetValuesFromProto(t)).ToList()
            };
            return block;
        }

        public object Clone()
        {
            return new Block((BlockHeader) BlockHeader.Clone(), Transactions.Select(item => (Transaction) item.Clone()).ToList());
        }
    }
}
