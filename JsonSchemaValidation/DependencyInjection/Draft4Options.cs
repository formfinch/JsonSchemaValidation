// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.DependencyInjection
{
    /// <summary>
    /// Options specific to JSON Schema Draft 4 validation.
    /// </summary>
    public class Draft4Options
    {
        /// <summary>
        /// When true, the 'format' keyword performs validation in addition to producing annotations.
        /// Default is true for Draft 4, as format was traditionally asserting in early drafts.
        /// </summary>
        public bool FormatAssertionEnabled { get; set; } = true;
    }
}
