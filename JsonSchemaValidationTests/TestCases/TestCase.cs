using System.Text.Json;
using System.Text.Json.Serialization;

namespace FormFinch.JsonSchemaValidationTests.TestCases
{
    public class TestCase
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("schema")]
        public JsonElement Schema { get; set; }

        [JsonPropertyName("tests")]
        public JsonElement[] Tests { get; set; } = Array.Empty<JsonElement>();
    }
}
