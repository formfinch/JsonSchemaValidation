// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.DependencyInjection
{
    public class SchemaValidationOptions
    {
        public string DefaultDraftVersion { get; set; } = "https://json-schema.org/draft/2020-12/schema";
        public bool EnableDraft202012 { get; set; } = true;
        public bool EnableDraft201909 { get; set; } = true;
        public bool EnableDraft7 { get; set; } = true;
        public bool EnableDraft6 { get; set; } = true;
        public bool EnableDraft4 { get; set; } = true;
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
