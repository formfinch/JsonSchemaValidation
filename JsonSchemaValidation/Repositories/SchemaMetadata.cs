using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Repositories
{
    public class SchemaMetadata
    {
        public JsonElement Schema { get; }
        public string? DraftVersion { get; set; }
        public Uri? SchemaUri { get; set; }

        public SchemaMetadata(JsonElement schema, string? draftVersion = null, Uri? schemaUri = null)
        {
            Schema = schema;
            DraftVersion = draftVersion;
            SchemaUri = schemaUri;
        }

        public SchemaMetadata(SchemaMetadata original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));

            Schema = original.Schema;
            DraftVersion = original.DraftVersion;
            if (original.SchemaUri != null)
            {
                SchemaUri = new Uri(original.SchemaUri.ToString());
            }
        }
    }
}
