using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Draft201909
{
    /// <summary>
    /// Registry mapping Draft 2019-09 vocabulary URIs to their associated keywords.
    /// </summary>
    public class VocabularyRegistry : IVocabularyRegistry
    {
        private readonly Dictionary<string, HashSet<string>> _vocabularyToKeywords;

        public VocabularyRegistry()
        {
            _vocabularyToKeywords = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
            {
                ["https://json-schema.org/draft/2019-09/vocab/core"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "$id", "$schema", "$ref", "$anchor", "$recursiveRef",
                    "$recursiveAnchor", "$vocabulary", "$comment", "$defs"
                },
                ["https://json-schema.org/draft/2019-09/vocab/applicator"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "additionalItems", "unevaluatedItems", "items", "contains",
                    "additionalProperties", "unevaluatedProperties", "properties",
                    "patternProperties", "dependentSchemas", "propertyNames",
                    "if", "then", "else", "allOf", "anyOf", "oneOf", "not"
                },
                ["https://json-schema.org/draft/2019-09/vocab/validation"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "type", "const", "enum", "multipleOf", "maximum", "exclusiveMaximum",
                    "minimum", "exclusiveMinimum", "maxLength", "minLength", "pattern",
                    "maxItems", "minItems", "uniqueItems", "maxContains", "minContains",
                    "maxProperties", "minProperties", "required", "dependentRequired"
                },
                ["https://json-schema.org/draft/2019-09/vocab/meta-data"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "title", "description", "default", "deprecated",
                    "readOnly", "writeOnly", "examples"
                },
                ["https://json-schema.org/draft/2019-09/vocab/format"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "format"
                },
                ["https://json-schema.org/draft/2019-09/vocab/content"] = new HashSet<string>(StringComparer.Ordinal)
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
