using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace BlockBase.Utils.Crypto
{
    public static class MerkleTreeHelper
    {
        public static byte[] CalculateMerkleRootHash(List<byte[]> leaves)
        {
            if (!leaves.Any())
                return new byte[32];
            if (leaves.Count == 1)
                return leaves.First();
            if ((leaves.Count % 2) > 0)
                leaves.Add(leaves.LastOrDefault());

            var branches = new List<byte[]>();
            
            for (int i = 0; i < leaves.Count; i+=2)
            {
                var concatenatedHash = (leaves[i].Concat(leaves[i+1])).ToArray();
                branches.Add(HashHelper.Sha256Data(concatenatedHash));
            }

            return CalculateMerkleRootHash(branches);
        }
    }
}