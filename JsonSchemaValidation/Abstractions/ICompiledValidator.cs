using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Abstractions;

/// <summary>
/// Interface for pre-compiled schema validators that provide fast-path validation.
/// Compiled validators only support IsValid (boolean result) - no detailed error reporting.
/// </summary>
/// <remarks>
/// Compiled validators are generated from JSON Schema files and registered by schema URI.
/// When validation is requested for a schema with a registered compiled validator,
/// the compiled validator is used instead of dynamically building validators.
/// </remarks>
public interface ICompiledValidator
{
    /// <summary>
    /// Gets the schema URI this compiled validator handles.
    /// </summary>
    Uri SchemaUri { get; }

    /// <summary>
    /// Validates the JSON instance against the compiled schema.
    /// </summary>
    /// <param name="instance">The JSON element to validate.</param>
    /// <returns>True if the instance is valid according to the schema; otherwise, false.</returns>
    bool IsValid(JsonElement instance);
}
