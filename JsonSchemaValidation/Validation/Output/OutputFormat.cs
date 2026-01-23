// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Validation.Output
{
    /// <summary>
    /// Output formats per JSON Schema 2020-12 Section 12.
    /// </summary>
    /// <remarks>
    /// <b>Thread safety:</b> This enum is immutable and safe for concurrent use.
    /// </remarks>
    public enum OutputFormat
    {
        /// <summary>
        /// Returns only the overall validation result (IsValid).
        /// Most efficient, no error details.
        /// </summary>
        Flag,

        /// <summary>
        /// Flat list of all errors with instance and keyword locations.
        /// Good for simple error reporting.
        /// </summary>
        Basic,

        /// <summary>
        /// Hierarchical nested structure matching schema structure.
        /// Best for complex schemas with allOf/anyOf/oneOf.
        /// </summary>
        Detailed
    }
}
