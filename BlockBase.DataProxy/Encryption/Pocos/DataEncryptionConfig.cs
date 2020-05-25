namespace BlockBase.DataProxy.Pocos
{
    public class DataEncryptionConfig 
    {
        public bool IsEncrypted { get; set; } 
        public string FilePassword { get; set; }
        public string EncryptionMasterKey { get; set; }
        public string EncryptionPassword { get; set; }

        public string PublicKey { get; set; }
        public string EncryptedData { get; set; }
    }
}