namespace JsonSchemaValidation.Draft202012
{
    /// <summary>
    /// Result of parsing $vocabulary from a schema.
    /// </summary>
    public class VocabularyParseResult
    {
        /// <summary>
        /// Map of vocabulary URIs to their required status (true = required, false = optional).
        /// </summary>
        public IDictionary<string, bool> Vocabularies { get; set; } = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>
        /// Set of active keyword names derived from the declared vocabularies.
        /// </summary>
        public ISet<string> ActiveKeywords { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    }
}
