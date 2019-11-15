namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class estring
    {
        public string Value { get; set; }
        public string EncryptedValue { get; set; }
        public bool ToEncrypt { get; set; }

        public estring() { }

        public estring(string value, bool toEncrypt)
        {
            Value = value;
            ToEncrypt = toEncrypt;
        }

        public estring(string encryptedValue)
        {
            EncryptedValue = encryptedValue;
            ToEncrypt = false;
        }

        public estring Clone()
        {
            return new estring() { Value = Value, ToEncrypt = ToEncrypt, EncryptedValue = EncryptedValue };
        }

        public string GetFinalString()
        {
            return EncryptedValue ?? Value;
        }
    }
}