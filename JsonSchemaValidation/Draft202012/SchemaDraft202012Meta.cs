using System.Text.Json;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Draft202012
{
    public class SchemaDraft202012Meta : ISchemaDraftMeta
    {
        private static readonly List<JsonElement> _schemas = new List<JsonElement>();

        public string DraftVersion => "https://json-schema.org/draft/2020-12/schema";

        public IEnumerable<JsonElement> Schemas => _schemas;

        public SchemaDraft202012Meta()
        {
            if (_schemas.Any()) return;

            AddSchemaDocument(_schemas, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_schema);
            AddSchemaDocument(_schemas, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_core);
            AddSchemaDocument(_schemas, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_applicator);
            AddSchemaDocument(_schemas, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_unevaluated);
            AddSchemaDocument(_schemas, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_validation);
            AddSchemaDocument(_schemas, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_meta_data);
            AddSchemaDocument(_schemas, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_format_annotation);
            AddSchemaDocument(_schemas, JsonSchemaValidation.Properties.Resources.json_schema_draft202012_meta_content);
            AddSchemaDocument(_schemas, JsonSchemaValidation.Properties.Resources.nop_true);
        }

        private static void AddSchemaDocument(IList<JsonElement> schemas, string resourceString)
        {
            using JsonDocument document = JsonDocument.Parse(resourceString);
            schemas.Add(document.RootElement.Clone());
        }
    }
}
