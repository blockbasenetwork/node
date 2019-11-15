using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Blockchain
{
    //TODO: Remove class, probably not necessary
    public class MerkleTreeNode
    {
        public byte[] MerkleTreeNodeHash { get; set; }
        //TODO See if list of transactions is needed seeing that only the bottom leafs will have transactions 
        //TODO verificar se implmentamos  public List<MerkleTreeNode> Children { get; set; }
        public List<Transaction> Transactions { get; set; }
        
        public MerkleTreeNode()
        {
            MerkleTreeNodeHash = new byte[10];
            Transactions = new List<Transaction>();
        }

        public void CalculateHash()
        {
            throw new NotImplementedException();
        }
    }
}