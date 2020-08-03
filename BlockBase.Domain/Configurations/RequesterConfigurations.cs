using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    public class RequesterConfigurations
    {

        public NodeConfig ValidatorNodes { get; set; }
        public NodeConfig HistoryNodes { get; set; }
        public NodeConfig FullNodes { get; set; }

        public double RequesterStake { get; set; }
        public double MinimumProducerStake { get; set; }
        public uint BlockTimeInSeconds { get; set; }
        public uint MaxBlockSizeInBytes { get; set; }
        public List<string> ReservedProducerSeats { get; set; }

        public DatabaseSecurityConfigurations DatabaseSecurityConfigurations { get; set; }

    }

    public class NodeConfig
    {
        public uint RequiredNumber { get; set; }
        public double MaxPaymentPerBlock { get; set; }
        public double MinPaymentPerBlock { get; set; }
    }

    public class DatabaseSecurityConfigurations
    {
        public bool Use { get; set; }
        public string FilePassword { get; set; }
        public string EncryptionMasterKey { get; set; }
        public string EncryptionPassword { get; set; }

        public bool IsEncrypted { get; set; }
        public string PublicKey { get; set; }
        public string EncryptedData { get; set; }
    }
}
