using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Draft201909
{
    public class SchemaDraft201909Meta : ISchemaDraftMeta
    {
        private static readonly Lazy<IReadOnlyList<JsonElement>> _schemas =
            new Lazy<IReadOnlyList<JsonElement>>(LoadSchemas);

        public string DraftVersion => "https://json-schema.org/draft/2019-09/schema";

        public IEnumerable<JsonElement> Schemas => _schemas.Value;

        private static IReadOnlyList<JsonElement> LoadSchemas()
        {
            var list = new List<JsonElement>();
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft201909_schema);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft201909_meta_core);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft201909_meta_applicator);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft201909_meta_validation);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft201909_meta_meta_data);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft201909_meta_format);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft201909_meta_content);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft201909_nop_true);
            return list.AsReadOnly();
        }

        private static void AddSchemaDocument(IList<JsonElement> schemas, string resourceString)
        {
            using JsonDocument document = JsonDocument.Parse(resourceString);
            schemas.Add(document.RootElement.Clone());
        }
    }
}
