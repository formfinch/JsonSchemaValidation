// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Collections.Concurrent;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.Repositories
{
    internal class SchemaMetadata
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
        public IDictionary<string, bool>? ActiveVocabularies { get; set; }

        /// <summary>
        /// Set of active keyword names derived from ActiveVocabularies.
        /// null means all keywords are active (default behavior).
        /// </summary>
        public ISet<string>? ActiveKeywords { get; set; }

        /// <summary>
        /// Indicates whether this schema has $recursiveAnchor: true (Draft 2019-09).
        /// Used for dynamic reference resolution with $recursiveRef.
        /// </summary>
        public bool HasRecursiveAnchor { get; set; }

        public SchemaMetadata(JsonElement schema, string? draftVersion = null, Uri? schemaUri = null)
        {
            Schema = schema;
            SchemaUri = schemaUri ?? SchemaRepositoryHelpers.ExtractSchemaUri(schema);
            DraftVersion = draftVersion ?? SchemaRepositoryHelpers.ExtractDraftVersion(schema);
            References = new ConcurrentDictionary<Uri, SchemaMetadata>(new UriWithFragmentComparer());
            Anchors = new ConcurrentDictionary<string, JsonElement>(StringComparer.Ordinal);
            DynamicAnchors = new ConcurrentDictionary<string, JsonElement>(StringComparer.Ordinal);
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
            Anchors = new ConcurrentDictionary<string, JsonElement>(originalSchemaData.Anchors, StringComparer.Ordinal);
            DynamicAnchors = new ConcurrentDictionary<string, JsonElement>(originalSchemaData.DynamicAnchors, StringComparer.Ordinal);

            ActiveVocabularies = originalSchemaData.ActiveVocabularies != null
                ? new Dictionary<string, bool>(originalSchemaData.ActiveVocabularies, StringComparer.Ordinal)
                : null;
            ActiveKeywords = originalSchemaData.ActiveKeywords != null
                ? new HashSet<string>(originalSchemaData.ActiveKeywords, StringComparer.Ordinal)
                : null;
            HasRecursiveAnchor = originalSchemaData.HasRecursiveAnchor;
        }
    }
}
