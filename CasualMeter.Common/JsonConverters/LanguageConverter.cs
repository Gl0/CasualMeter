using System;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CasualMeter.Common.JsonConverters
{
    public class LanguageConverter : JsonConverter
    {
        private List<string> Languages = new List<string> { "Auto", "NA", "RU", "TW", "JP", "KR", "EU-EN", "EU-FR", "EU-GER" };
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(string));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            return (token.Value<string>() == null) ? "Auto" : (Languages.Contains(token.Value<string>())) ? token.Value<string>() : "Auto";
        }
    }
}