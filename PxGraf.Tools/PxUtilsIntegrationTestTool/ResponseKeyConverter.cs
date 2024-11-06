using Newtonsoft.Json;

namespace Tools.PxUtilsIntegrationTestTool
{
    internal class ResponseKeyConverter : JsonConverter<Dictionary<ResponsesKey, List<string>>>
    {
        public override Dictionary<ResponsesKey, List<string>>? ReadJson(JsonReader reader, Type objectType, Dictionary<ResponsesKey, List<string>>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("This converter is read only");
        }

        public override void WriteJson(JsonWriter writer, Dictionary<ResponsesKey, List<string>> value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (var pair in value)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Sq");
                WriteResponsesToJson(writer, pair.Key.Sq);
                writer.WritePropertyName("SqMeta");
                WriteResponsesToJson(writer, pair.Key.SqMeta);
                writer.WritePropertyName("SqVisualization");
                WriteResponsesToJson(writer, pair.Key.SqVisualization);
                writer.WritePropertyName("Queries");
                serializer.Serialize(writer, pair.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        private static void WriteResponsesToJson(JsonWriter writer, Responses responses)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("PxUtilsResponseFound");
            writer.WriteValue(responses.PxUtilsResponseFound);
            writer.WritePropertyName("PxWebApiResponseFound");
            writer.WriteValue(responses.PxWebApiResponseFound);
            writer.WritePropertyName("PxWebApiOldResponseFound");
            writer.WriteValue(responses.PxWebApiOldResponseFound);
            writer.WriteEndObject();
        }
    }
}
