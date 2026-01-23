// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Abstractions
{
    /// <summary>
    /// Registry that maps vocabulary URIs to their associated keywords.
    /// Used for $vocabulary keyword support in Draft 2020-12.
    /// </summary>
    internal interface IVocabularyRegistry
    {
        /// <summary>
        /// Gets all keywords associated with a vocabulary URI.
        /// </summary>
        /// <param name="vocabularyUri">The vocabulary URI (e.g., "https://json-schema.org/draft/2020-12/vocab/validation")</param>
        /// <returns>Set of keyword names, or null if vocabulary is not recognized</returns>
        IReadOnlySet<string>? GetKeywordsForVocabulary(string vocabularyUri);

        /// <summary>
        /// Checks if a vocabulary is recognized/supported by this validator.
        /// </summary>
        /// <param name="vocabularyUri">The vocabulary URI to check</param>
        /// <returns>True if the vocabulary is supported</returns>
        bool IsVocabularySupported(string vocabularyUri);
    }
}
