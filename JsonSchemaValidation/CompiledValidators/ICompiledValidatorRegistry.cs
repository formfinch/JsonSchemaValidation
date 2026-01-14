using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.CompiledValidators;

/// <summary>
/// Registry for compiled validators indexed by schema URI.
/// </summary>
public interface ICompiledValidatorRegistry
{
    /// <summary>
    /// Registers a compiled validator for its schema URI.
    /// If a validator is already registered for the URI, it will be replaced.
    /// </summary>
    /// <param name="validator">The compiled validator to register.</param>
    void Register(ICompiledValidator validator);

    /// <summary>
    /// Attempts to get a compiled validator for the given schema URI.
    /// </summary>
    /// <param name="schemaUri">The schema URI to look up.</param>
    /// <param name="validator">The compiled validator if found; otherwise, null.</param>
    /// <returns>True if a compiled validator was found; otherwise, false.</returns>
    bool TryGetValidator(Uri schemaUri, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ICompiledValidator? validator);

    /// <summary>
    /// Checks if a compiled validator exists for the given schema URI.
    /// </summary>
    /// <param name="schemaUri">The schema URI to check.</param>
    /// <returns>True if a compiled validator is registered; otherwise, false.</returns>
    bool HasValidator(Uri schemaUri);
}
