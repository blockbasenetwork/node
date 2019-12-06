namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class estring
    {
        public string Value { get; set; }
        public bool ToEncrypt { get; set; }

        public estring()
        {
            ToEncrypt = false;
        }

        public estring(string value, bool toEncrypt)
        {
            Value = value;
            ToEncrypt = toEncrypt;
        }

        public estring(string value)
        {
            Value = value;
            ToEncrypt = false;
        }


        public estring Clone()
        {
            return new estring() { Value = Value, ToEncrypt = ToEncrypt};
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                estring e = (estring)obj;
                return Value == e.Value && ToEncrypt == e.ToEncrypt;
            }
        }

        public override int GetHashCode()
        {
            return (Value + ToEncrypt).GetHashCode();
        }
    }
}