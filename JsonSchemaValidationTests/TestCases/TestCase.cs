// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
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
