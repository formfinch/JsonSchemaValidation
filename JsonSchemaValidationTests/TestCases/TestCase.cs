using System.Text.Json.Serialization;
using System.Text.Json;

namespace JsonSchemaValidationTests.TestCases
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
