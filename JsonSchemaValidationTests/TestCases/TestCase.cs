using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

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
