namespace BlockBase.DataProxy.Encryption
{
    //TODO - this interface should be implemented by a class that provides access to a secure system where the database keys are stored
    public interface ISecretStore
    {
        void SetSecret(string keyId, byte[] key);

        void RemoveSecret(string keyId);

        byte[] GetSecret(string keyId);
    }
}