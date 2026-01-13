using System.Text.Json;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Draft4
{
    public class SchemaDraft4Meta : ISchemaDraftMeta
    {
        private static readonly Lazy<IReadOnlyList<JsonElement>> _schemas =
            new Lazy<IReadOnlyList<JsonElement>>(LoadSchemas);

        public string DraftVersion => "http://json-schema.org/draft-04/schema";

        public IEnumerable<JsonElement> Schemas => _schemas.Value;

        private static IReadOnlyList<JsonElement> LoadSchemas()
        {
            var list = new List<JsonElement>();
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft4_schema);
            AddSchemaDocument(list, JsonSchemaValidation.Properties.Resources.json_schema_draft4_nop_true);
            return list.AsReadOnly();
        }

        private static void AddSchemaDocument(IList<JsonElement> schemas, string resourceString)
        {
            using JsonDocument document = JsonDocument.Parse(resourceString);
            schemas.Add(document.RootElement.Clone());
        }
    }
}
