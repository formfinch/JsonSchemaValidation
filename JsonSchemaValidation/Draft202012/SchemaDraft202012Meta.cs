// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Draft202012
{
    internal class SchemaDraft202012Meta : ISchemaDraftMeta
    {
        private static readonly Lazy<IReadOnlyList<JsonElement>> _schemas =
            new Lazy<IReadOnlyList<JsonElement>>(LoadSchemas);

        public string DraftVersion => "https://json-schema.org/draft/2020-12/schema";

        public IEnumerable<JsonElement> Schemas => _schemas.Value;

        private static IReadOnlyList<JsonElement> LoadSchemas()
        {
            var list = new List<JsonElement>();
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_schema);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_core);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_applicator);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_unevaluated);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_validation);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_meta_data);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_format_annotation);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_content);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.nop_true);
            return list.AsReadOnly();
        }

        private static void AddSchemaDocument(IList<JsonElement> schemas, string resourceString)
        {
            using JsonDocument document = JsonDocument.Parse(resourceString);
            schemas.Add(document.RootElement.Clone());
        }
    }
}
