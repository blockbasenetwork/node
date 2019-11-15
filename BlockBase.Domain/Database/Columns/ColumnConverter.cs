using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Columns
{
    public class ColumnConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Column));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            ColumnType enumValue = (ColumnType)Enum.ToObject(typeof(ColumnType), jo["ColumnType"].Value<int>());
            if (enumValue == ColumnType.ForeignColumn)
                return jo.ToObject<ForeignColumn>(serializer);

            if (enumValue == ColumnType.NormalColumn)
                return jo.ToObject<NormalColumn>(serializer);

            if (enumValue == ColumnType.PrimaryColumn)
                return jo.ToObject<PrimaryColumn>(serializer);

            if (enumValue == ColumnType.RangeColumn)
                return jo.ToObject<RangeColumn>(serializer);

            if (enumValue == ColumnType.UniqueColumn)
                return jo.ToObject<UniqueColumn>(serializer);

            return jo.ToObject<NormalColumn>(serializer); 
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
