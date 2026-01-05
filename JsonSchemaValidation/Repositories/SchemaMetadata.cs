using System.Collections.Concurrent;
using System.Text.Json;
using JsonSchemaValidation.Common;

namespace JsonSchemaValidation.Repositories
{
    public class SchemaMetadata
    {
        public JsonElement Schema { get; set; }
        public string? DraftVersion { get; set; }
        public Uri? SchemaUri { get; set; }
        public ConcurrentDictionary<string, JsonElement> Anchors { get; set; }
        public ConcurrentDictionary<string, JsonElement> DynamicAnchors { get; set; }
        public ConcurrentDictionary<Uri, SchemaMetadata> References { get; set; }
        public int CyclicReference { get; set; } = 0;
        public int Order { get; set; }

        /// <summary>
        /// Active vocabularies for this schema, mapped from vocabulary URI to required flag.
        /// null means default vocabularies (all standard vocabularies active).
        /// </summary>
        public Dictionary<string, bool>? ActiveVocabularies { get; set; }

        /// <summary>
        /// Set of active keyword names derived from ActiveVocabularies.
        /// null means all keywords are active (default behavior).
        /// </summary>
        public HashSet<string>? ActiveKeywords { get; set; }

        public SchemaMetadata(JsonElement schema, string? draftVersion = null, Uri? schemaUri = null)
        {
            Schema = schema;
            SchemaUri = schemaUri ?? SchemaRepositoryHelpers.ExtractSchemaUri(schema);
            DraftVersion = draftVersion ?? SchemaRepositoryHelpers.ExtractDraftVersion(schema);
            References = new ConcurrentDictionary<Uri, SchemaMetadata>(new UriWithFragmentComparer());
            Anchors = new ConcurrentDictionary<string, JsonElement>();
            DynamicAnchors = new ConcurrentDictionary<string, JsonElement>();
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
            if (originalSchemaData.References != null && originalSchemaData.References.Count != 0)
            {
                References = new ConcurrentDictionary<Uri, SchemaMetadata>(originalSchemaData.References, new UriWithFragmentComparer());
            }
            else
            {
                References = new ConcurrentDictionary<Uri, SchemaMetadata>(new UriWithFragmentComparer());
            }
            Anchors = new ConcurrentDictionary<string, JsonElement>(originalSchemaData.Anchors);
            DynamicAnchors = new ConcurrentDictionary<string, JsonElement>(originalSchemaData.DynamicAnchors);

            ActiveVocabularies = originalSchemaData.ActiveVocabularies != null
                ? new Dictionary<string, bool>(originalSchemaData.ActiveVocabularies)
                : null;
            ActiveKeywords = originalSchemaData.ActiveKeywords != null
                ? new HashSet<string>(originalSchemaData.ActiveKeywords)
                : null;
        }
    }
}
