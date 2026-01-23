// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Abstractions
{
    /// <summary>
    /// Result of parsing $vocabulary from a schema.
    /// </summary>
    internal class VocabularyParseResult
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
