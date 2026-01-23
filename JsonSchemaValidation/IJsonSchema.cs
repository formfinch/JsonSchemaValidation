// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Validation.Output;

namespace FormFinch.JsonSchemaValidation
{
    /// <summary>
    /// Represents a parsed JSON Schema that can validate multiple instances efficiently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="JsonSchemaValidator.Parse(string)"/> or
    /// <see cref="JsonSchemaValidator.Parse(JsonElement)"/> to create instances.
    /// </para>
    /// <para>
    /// Parsed schemas are more efficient than one-shot validation when validating
    /// multiple instances against the same schema, as the schema is parsed and
    /// registered only once.
    /// </para>
    /// <para>
    /// <b>Thread safety:</b> Parsed schema instances are safe for concurrent validation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var schema = JsonSchemaValidator.Parse("""{"type": "string", "minLength": 1}""");
    ///
    /// // Validate multiple instances
    /// var result1 = schema.Validate("\"hello\"");
    /// var result2 = schema.Validate("\"world\"");
    /// var result3 = schema.Validate("\"\""); // Will be invalid (minLength: 1)
    /// </code>
    /// </example>
    public interface IJsonSchema
    {
        /// <summary>
        /// Gets the URI that identifies this schema.
        /// </summary>
        Uri SchemaUri { get; }

        /// <summary>
        /// Validates a JSON instance against this schema.
        /// </summary>
        /// <param name="instanceJson">The JSON instance to validate as a string.</param>
        /// <param name="format">The output format. Defaults to <see cref="OutputFormat.Basic"/>.</param>
        /// <returns>The validation result in the specified output format.</returns>
        /// <exception cref="JsonException">Thrown when the instance JSON is invalid.</exception>
        OutputUnit Validate(string instanceJson, OutputFormat format = OutputFormat.Basic);

        /// <summary>
        /// Validates a JSON instance against this schema.
        /// </summary>
        /// <param name="instance">The JSON instance to validate.</param>
        /// <param name="format">The output format. Defaults to <see cref="OutputFormat.Basic"/>.</param>
        /// <returns>The validation result in the specified output format.</returns>
        OutputUnit Validate(JsonElement instance, OutputFormat format = OutputFormat.Basic);

        /// <summary>
        /// Checks if a JSON instance is valid against this schema.
        /// This is more efficient than <see cref="Validate(string, OutputFormat)"/>
        /// when you only need a boolean result.
        /// </summary>
        /// <param name="instanceJson">The JSON instance to validate as a string.</param>
        /// <returns><c>true</c> if the instance is valid; otherwise, <c>false</c>.</returns>
        /// <exception cref="JsonException">Thrown when the instance JSON is invalid.</exception>
        bool IsValid(string instanceJson);

        /// <summary>
        /// Checks if a JSON instance is valid against this schema.
        /// This is more efficient than <see cref="Validate(JsonElement, OutputFormat)"/>
        /// when you only need a boolean result.
        /// </summary>
        /// <param name="instance">The JSON instance to validate.</param>
        /// <returns><c>true</c> if the instance is valid; otherwise, <c>false</c>.</returns>
        bool IsValid(JsonElement instance);
    }
}
