using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012
{
    public class SchemaDraft202012Meta : ISchemaDraftMeta
    {
        private readonly JsonElement _schemaDraft202012Meta;

        public string DraftVersion => "https://json-schema.org/draft/2020-12/schema";

        public JsonElement MetaSchema => _schemaDraft202012Meta;

        public SchemaDraft202012Meta() {

            using JsonDocument document = JsonDocument.Parse(JsonSchemaValidation.Properties.Resources.json_schema_draft202012);
            _schemaDraft202012Meta = document.RootElement.Clone();
        }
    }
}
