// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.DependencyInjection;

// Intentionally in root namespace for public API usability (file stays in DependencyInjection/ for organization).
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace FormFinch.JsonSchemaValidation
#pragma warning restore IDE0130
{
    /// <summary>
    /// Options that control JSON Schema validation behavior and supported drafts.
    /// </summary>
    /// <remarks>
    /// <b>Thread safety:</b> This type is mutable and not thread-safe. Configure it during
    /// startup and treat it as immutable once passed to validation services.
    /// </remarks>
    public class SchemaValidationOptions
    {
        /// <summary>
        /// Gets or sets the default JSON Schema draft version URI used when a schema does not declare <c>$schema</c>.
        /// Defaults to Draft 2020-12.
        /// </summary>
        public string DefaultDraftVersion { get; set; } = "https://json-schema.org/draft/2020-12/schema";

        /// <summary>
        /// Gets or sets a value indicating whether JSON Schema Draft 2020-12 is enabled. Default is <see langword="true"/>.
        /// </summary>
        public bool EnableDraft202012 { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether JSON Schema Draft 2019-09 is enabled. Default is <see langword="true"/>.
        /// </summary>
        public bool EnableDraft201909 { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether JSON Schema Draft 7 is enabled. Default is <see langword="true"/>.
        /// </summary>
        public bool EnableDraft7 { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether JSON Schema Draft 6 is enabled. Default is <see langword="true"/>.
        /// </summary>
        public bool EnableDraft6 { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether JSON Schema Draft 4 is enabled. Default is <see langword="true"/>.
        /// </summary>
        public bool EnableDraft4 { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether JSON Schema Draft 3 is enabled. Default is <see langword="true"/>.
        /// </summary>
        public bool EnableDraft3 { get; set; } = true;

        /// <summary>
        /// Draft-specific options for JSON Schema Draft 2020-12.
        /// </summary>
        public Draft202012Options Draft202012 { get; set; } = new();

        /// <summary>
        /// Draft-specific options for JSON Schema Draft 2019-09.
        /// </summary>
        public Draft201909Options Draft201909 { get; set; } = new();

        /// <summary>
        /// Draft-specific options for JSON Schema Draft 7.
        /// </summary>
        public Draft7Options Draft7 { get; set; } = new();

        /// <summary>
        /// Draft-specific options for JSON Schema Draft 6.
        /// </summary>
        public Draft6Options Draft6 { get; set; } = new();

        /// <summary>
        /// Draft-specific options for JSON Schema Draft 4.
        /// </summary>
        public Draft4Options Draft4 { get; set; } = new();

        /// <summary>
        /// Draft-specific options for JSON Schema Draft 3.
        /// </summary>
        public Draft3Options Draft3 { get; set; } = new();
    }
}
