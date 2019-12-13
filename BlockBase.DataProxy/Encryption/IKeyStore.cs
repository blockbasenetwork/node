namespace BlockBase.DataProxy.Encryption
{
    //TODO - this interface should be implemented by a class that provides access to a secure system where the database keys are stored
    public interface IKeyStore
    {
        void SetKey(string keyId, byte[] key);

        void RemoveKey(string keyId);

        byte[] GetKey(string keyId);
    }
}