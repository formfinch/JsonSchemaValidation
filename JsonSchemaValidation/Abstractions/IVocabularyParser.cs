// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    /// <summary>
    /// Interface for parsing $vocabulary from meta-schemas.
    /// </summary>
    internal interface IVocabularyParser
    {
        /// <summary>
        /// The draft version this parser supports (e.g., "https://json-schema.org/draft/2019-09/schema").
        /// </summary>
        string DraftVersion { get; }

        /// <summary>
        /// Parses the $vocabulary keyword from a meta-schema.
        /// </summary>
        /// <param name="schema">The schema element to parse</param>
        /// <returns>Parse result with vocabulary info, or null if no $vocabulary is present</returns>
        VocabularyParseResult? ParseVocabulary(JsonElement schema);
    }
}
