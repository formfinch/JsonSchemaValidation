// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.DependencyInjection
{
    /// <summary>
    /// Options specific to JSON Schema Draft 2020-12 validation.
    /// </summary>
    public class Draft202012Options
    {
        /// <summary>
        /// When true, the 'format' keyword performs validation in addition to producing annotations.
        /// Default is false for Draft 2020-12, as format is annotation-only per spec.
        /// Format assertion can also be enabled via the format-assertion vocabulary in the schema.
        /// </summary>
        public bool FormatAssertionEnabled { get; set; } = false;
    }
}
