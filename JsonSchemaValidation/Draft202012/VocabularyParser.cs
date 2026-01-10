using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Exceptions;

namespace JsonSchemaValidation.Draft202012
{
    /// <summary>
    /// Parses $vocabulary declarations from meta-schemas.
    /// </summary>
    public class VocabularyParser
    {
        private readonly IVocabularyRegistry _registry;

        public VocabularyParser(IVocabularyRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Parses the $vocabulary keyword from a meta-schema.
        /// </summary>
        /// <param name="schema">The schema element to parse</param>
        /// <returns>Parse result with vocabulary info, or null if no $vocabulary is present</returns>
        /// <exception cref="InvalidSchemaException">Thrown when a required vocabulary is not supported</exception>
        public VocabularyParseResult? ParseVocabulary(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("$vocabulary", out var vocabElement))
            {
                return null;
            }

            if (vocabElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var vocabularies = new Dictionary<string, bool>(StringComparer.Ordinal);
            var activeKeywords = new HashSet<string>(StringComparer.Ordinal);
            var unsupportedRequired = new List<string>();

            foreach (var prop in vocabElement.EnumerateObject())
            {
                string vocabUri = prop.Name;

                if (prop.Value.ValueKind != JsonValueKind.True && prop.Value.ValueKind != JsonValueKind.False)
                {
                    continue;
                }

                bool isRequired = prop.Value.GetBoolean();
                vocabularies[vocabUri] = isRequired;

                if (_registry.IsVocabularySupported(vocabUri))
                {
                    var keywords = _registry.GetKeywordsForVocabulary(vocabUri);
                    if (keywords != null)
                    {
                        for (int i = 0; keywords.Skip(i).Any(); i++)
                        {
                            activeKeywords.Add(keywords.ElementAt(i));
                        }
                    }
                }
                else if (isRequired)
                {
                    unsupportedRequired.Add(vocabUri);
                }
                // If optional (false) and unsupported, silently ignore per spec
            }

            if (unsupportedRequired.Count > 0)
            {
                throw new InvalidSchemaException(
                    $"Schema requires unsupported vocabularies: {string.Join(", ", unsupportedRequired)}");
            }

            return new VocabularyParseResult
            {
                Vocabularies = vocabularies,
                ActiveKeywords = activeKeywords
            };
        }
    }
}
