namespace BlockBase.Domain.Configurations
{
    public class SecurityConfigurations 
    {
        public bool UseSecurityConfigurations { get; set; }
        public string FilePassword { get; set; }
        public string EncryptionMasterKey { get; set; }
        public string EncryptionPassword { get; set; }

        public bool IsEncrypted { get; set; } 
        public string PublicKey { get; set; }
        public string EncryptedData { get; set; }
    }
}