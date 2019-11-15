using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Records
{
    public class RecordConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Record));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["RecordType"].Value<int>() == 0)
                return jo.ToObject<BoolRecord>(serializer);
            if (jo["RecordType"].Value<int>() == 1)
                return jo.ToObject<GuidRecord>(serializer);
            if (jo["RecordType"].Value<int>() == 2)
                return jo.ToObject<IntRecord>(serializer);
            if (jo["RecordType"].Value<int>() == 3)
                return jo.ToObject<StringRecord>(serializer);
            if (jo["RecordType"].Value<int>() == 4)
                return jo.ToObject<VarBinaryRecord>(serializer);

            return null;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
