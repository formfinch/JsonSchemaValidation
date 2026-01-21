// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Abstractions.Keywords
{
    /// <summary>
    /// Factory interface for draft-specific keyword validators.
    /// Implementations should be registered with keyed services using the draft version as key.
    /// </summary>
    public interface ISchemaDraftKeywordValidatorFactory
    {
        /// <summary>
        /// The keyword name this factory handles (e.g., "minimum", "properties").
        /// Used for vocabulary-based filtering.
        /// </summary>
        string Keyword { get; }

        /// <summary>
        /// Determines the order in which validators are executed.
        /// Lower values execute first. Default is 0.
        /// Unevaluated keywords (unevaluatedItems, unevaluatedProperties) should use higher values
        /// to ensure they run after other applicator keywords.
        /// </summary>
        int ExecutionOrder => 0;

        /// <summary>
        /// Creates a keyword validator for the given schema metadata.
        /// </summary>
        /// <param name="schemaData">The schema metadata to create a validator for.</param>
        /// <returns>A keyword validator, or null if this factory doesn't apply to the schema.</returns>
        IKeywordValidator? Create(SchemaMetadata schemaData);
    }
}
