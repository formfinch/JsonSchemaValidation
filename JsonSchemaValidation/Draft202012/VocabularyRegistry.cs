// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Draft202012
{
    /// <summary>
    /// Registry mapping Draft 2020-12 vocabulary URIs to their associated keywords.
    /// </summary>
    public class VocabularyRegistry : IVocabularyRegistry
    {
        private readonly Dictionary<string, HashSet<string>> _vocabularyToKeywords;

        public VocabularyRegistry()
        {
            _vocabularyToKeywords = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
            {
                ["https://json-schema.org/draft/2020-12/vocab/core"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "$id", "$schema", "$ref", "$anchor", "$dynamicRef",
                    "$dynamicAnchor", "$vocabulary", "$comment", "$defs"
                },
                ["https://json-schema.org/draft/2020-12/vocab/applicator"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "prefixItems", "items", "contains", "additionalProperties",
                    "properties", "patternProperties", "dependentSchemas",
                    "propertyNames", "if", "then", "else", "allOf", "anyOf", "oneOf", "not"
                },
                ["https://json-schema.org/draft/2020-12/vocab/validation"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "type", "const", "enum", "multipleOf", "maximum", "exclusiveMaximum",
                    "minimum", "exclusiveMinimum", "maxLength", "minLength", "pattern",
                    "maxItems", "minItems", "uniqueItems", "maxContains", "minContains",
                    "maxProperties", "minProperties", "required", "dependentRequired"
                },
                ["https://json-schema.org/draft/2020-12/vocab/unevaluated"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "unevaluatedItems", "unevaluatedProperties"
                },
                ["https://json-schema.org/draft/2020-12/vocab/meta-data"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "title", "description", "default", "deprecated",
                    "readOnly", "writeOnly", "examples"
                },
                ["https://json-schema.org/draft/2020-12/vocab/format-annotation"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "format"
                },
                ["https://json-schema.org/draft/2020-12/vocab/format-assertion"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "format"
                },
                ["https://json-schema.org/draft/2020-12/vocab/content"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "contentEncoding", "contentMediaType", "contentSchema"
                }
            };
        }

        /// <inheritdoc />
        public IReadOnlySet<string>? GetKeywordsForVocabulary(string vocabularyUri)
        {
            if (_vocabularyToKeywords.TryGetValue(vocabularyUri, out var keywords))
            {
                return keywords;
            }
            return null;
        }

        /// <inheritdoc />
        public bool IsVocabularySupported(string vocabularyUri)
        {
            return _vocabularyToKeywords.ContainsKey(vocabularyUri);
        }
    }
}
