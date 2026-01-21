// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Validation.Output
{
    /// <summary>
    /// Represents a single output unit per JSON Schema 2020-12 Section 12.
    /// Used for spec-compliant output formatting.
    /// </summary>
    public class OutputUnit
    {
        /// <summary>
        /// Whether this validation unit passed.
        /// </summary>
        public required bool Valid { get; set; }

        /// <summary>
        /// JSON Pointer to the instance location being validated.
        /// Example: "/users/0/email"
        /// </summary>
        public required string InstanceLocation { get; set; }

        /// <summary>
        /// JSON Pointer to the schema keyword that produced this result.
        /// Example: "/properties/users/items/properties/email/format"
        /// </summary>
        public required string KeywordLocation { get; set; }

        /// <summary>
        /// Absolute URI of the schema keyword including fragment.
        /// Example: "https://example.com/schema#/properties/users/items"
        /// </summary>
        public string? AbsoluteKeywordLocation { get; set; }

        /// <summary>
        /// Error message when Valid is false.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Annotation value when Valid is true and this keyword produces annotations.
        /// </summary>
        public object? Annotation { get; set; }

        /// <summary>
        /// Nested error output units for Detailed format.
        /// </summary>
        public IList<OutputUnit>? Errors { get; set; }

        /// <summary>
        /// Nested annotation output units for Detailed format.
        /// </summary>
        public IList<OutputUnit>? Annotations { get; set; }
    }
}
