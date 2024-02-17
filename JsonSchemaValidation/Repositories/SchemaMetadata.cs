using JsonSchemaValidation.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Repositories
{
    public class SchemaMetadata
    {
        public JsonElement Schema { get; set; }
        public string? DraftVersion { get; set; }
        public Uri? SchemaUri { get; set; }
        public HashSet<Uri> References { get; set; }
        public ConcurrentDictionary<string, JsonElement> Anchors { get; set; }

        public SchemaMetadata(JsonElement schema, string? draftVersion = null, Uri? schemaUri = null)
        {
            Schema = schema;
            SchemaUri = schemaUri ?? SchemaRepositoryHelpers.ExtractSchemaUri(schema);
            DraftVersion = draftVersion ?? SchemaRepositoryHelpers.ExtractDraftVersion(schema);
            References = new HashSet<Uri>();
            Anchors = new ConcurrentDictionary<string, JsonElement>();
        }

        public SchemaMetadata(SchemaMetadata originalSchemaData)
        {
            if (originalSchemaData == null) throw new ArgumentNullException(nameof(originalSchemaData));

            Schema = originalSchemaData.Schema;
            DraftVersion = originalSchemaData.DraftVersion;
            if (originalSchemaData.SchemaUri != null)
            {
                SchemaUri = new Uri(originalSchemaData.SchemaUri.ToString());
            }
            if(originalSchemaData.References != null && originalSchemaData.References.Count != 0)
            {
                References = originalSchemaData.References.ToHashSet<Uri>(new UriWithFragmentComparer());
            }
            else
            {
                References = new HashSet<Uri>(new UriWithFragmentComparer());
            }
            Anchors = new ConcurrentDictionary<string, JsonElement>(originalSchemaData.Anchors);
        }
    }
}
