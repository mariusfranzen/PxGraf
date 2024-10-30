using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tools.PxUtilsIntegrationTestTool
{
    internal class WhitelistConverter : JsonConverter<Dictionary<WhitelistKey, List<WhitelistRule>>>
    {
        public override Dictionary<WhitelistKey, List<WhitelistRule>>? ReadJson(JsonReader reader, Type objectType, Dictionary<WhitelistKey, List<WhitelistRule>>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var whitelist = new Dictionary<WhitelistKey, List<WhitelistRule>>();
            var jArray = JArray.Load(reader);

            foreach (var jToken in jArray)
            {
                var jObject = (JObject)jToken;
                var key = new WhitelistKey
                {
                    ResponseToken = jObject["ResponseToken"]?.ToString() ?? string.Empty,
                    Path = jObject["Path"]?.ToString() ?? string.Empty
                };

                var rules = jObject["Rules"]?.ToObject<List<WhitelistRule>>(serializer) ?? new List<WhitelistRule>();
                whitelist[key] = rules;
            }

            return whitelist;
        }

        public override void WriteJson(JsonWriter writer, Dictionary<WhitelistKey, List<WhitelistRule>>? value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (var pair in value)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("ResponseToken");
                writer.WriteValue(pair.Key.ResponseToken);
                writer.WritePropertyName("Path");
                writer.WriteValue(pair.Key.Path);
                writer.WritePropertyName("Rules");
                serializer.Serialize(writer, pair.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
