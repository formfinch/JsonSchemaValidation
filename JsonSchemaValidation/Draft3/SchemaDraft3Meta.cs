// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Draft3
{
    internal class SchemaDraft3Meta : ISchemaDraftMeta
    {
        private static readonly Lazy<IReadOnlyList<JsonElement>> _schemas =
            new Lazy<IReadOnlyList<JsonElement>>(LoadSchemas);

        public string DraftVersion => "http://json-schema.org/draft-03/schema";

        public IEnumerable<JsonElement> Schemas => _schemas.Value;

        private static IReadOnlyList<JsonElement> LoadSchemas()
        {
            var list = new List<JsonElement>();
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft3_schema);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft3_nop_true);
            return list.AsReadOnly();
        }

        private static void AddSchemaDocument(IList<JsonElement> schemas, string resourceString)
        {
            using JsonDocument document = JsonDocument.Parse(resourceString);
            schemas.Add(document.RootElement.Clone());
        }
    }
}
