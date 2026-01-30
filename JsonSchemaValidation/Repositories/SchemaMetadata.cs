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

        /// <summary>
        /// Private constructor for CreateShallowView - does NOT allocate dictionaries.
        /// Caller must set References, Anchors, DynamicAnchors after construction.
        /// </summary>
        private SchemaMetadata(JsonElement schema, string? draftVersion, Uri? schemaUri, bool skipDictionaryAllocation)
        {
            Schema = schema;
            SchemaUri = schemaUri;
            DraftVersion = draftVersion;
            // Dictionaries left as null! - caller MUST set them via object initializer
            References = null!;
            Anchors = null!;
            DynamicAnchors = null!;
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

        /// <summary>
        /// Creates a shallow view of the parent metadata with a different Schema and SchemaUri.
        /// Shares dictionaries (References, Anchors, DynamicAnchors) with the parent to avoid allocation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// THREAD SAFETY: This method shares the parent's ConcurrentDictionary instances
        /// (References, Anchors, DynamicAnchors) with the returned view. This is safe because:
        /// </para>
        /// <list type="bullet">
        /// <item>These dictionaries are populated ONLY during schema registration (TryRegisterSchema/WalkElement)</item>
        /// <item>Schema registration completes before any validation begins</item>
        /// <item>During validation, all access to these dictionaries is read-only (TryGetValue, ContainsKey)</item>
        /// <item>ConcurrentDictionary supports concurrent reads without synchronization</item>
        /// </list>
        /// <para>
        /// The shallow view pattern is used in SchemaRepository.GetSchema and DynamicRefValidator
        /// to avoid allocating 5+ dictionaries per $ref/$dynamicRef resolution.
        /// </para>
        /// </remarks>
        internal static SchemaMetadata CreateShallowView(SchemaMetadata parent, JsonElement schema, Uri? schemaUri)
        {
            ArgumentNullException.ThrowIfNull(parent);

            // Use private constructor that skips dictionary allocation.
            // Share dictionaries - safe because they are immutable after schema registration.
            // See class remarks for thread safety guarantees.
            return new SchemaMetadata(schema, parent.DraftVersion, schemaUri, skipDictionaryAllocation: true)
            {
                References = parent.References,
                Anchors = parent.Anchors,
                DynamicAnchors = parent.DynamicAnchors,
                ActiveVocabularies = parent.ActiveVocabularies,
                ActiveKeywords = parent.ActiveKeywords,
                HasRecursiveAnchor = parent.HasRecursiveAnchor,
                Order = parent.Order
            };
        }

        /// <summary>
        /// Cache for URIs with fragments, to avoid repeated allocations.
        /// </summary>
        private ConcurrentDictionary<string, Uri>? _fragmentUriCache;

        /// <summary>
        /// Gets or creates a URI combining SchemaUri with the specified fragment.
        /// Cached to avoid repeated Uri allocations for the same fragment.
        /// </summary>
        /// <param name="fragment">The fragment to append (e.g., "#foo").</param>
        /// <returns>Combined URI, or null if SchemaUri is null.</returns>
        internal Uri? GetUriWithFragment(string fragment)
        {
            if (SchemaUri == null)
            {
                return null;
            }

            _fragmentUriCache ??= new ConcurrentDictionary<string, Uri>(StringComparer.Ordinal);

            // Use GetOrAdd with static lambda + argument to avoid both:
            // 1. Closure allocation (lambda captures SchemaUri via argument, not 'this')
            // 2. Race condition (GetOrAdd is atomic - no check-then-act gap)
            return _fragmentUriCache.GetOrAdd(
                fragment,
                static (f, baseUri) => new Uri(baseUri, f),
                SchemaUri);
        }
    }
}
