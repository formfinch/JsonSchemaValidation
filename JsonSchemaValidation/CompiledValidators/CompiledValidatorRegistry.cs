using System.Diagnostics.CodeAnalysis;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.CompiledValidators;

/// <summary>
/// Thread-safe registry for compiled validators indexed by schema URI.
/// Uses string keys for faster lookups (Uri equality is slow).
/// </summary>
public sealed class CompiledValidatorRegistry : ICompiledValidatorRegistry
{
    // Use Dictionary with string keys for faster lookups.
    // Registry is populated once during initialization and only read during validation.
    private readonly Dictionary<string, ICompiledValidator> _validators = new(StringComparer.Ordinal);

    // Track registered hosts for quick rejection of non-matching URIs
    private readonly HashSet<string> _registeredHosts = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Register(ICompiledValidator validator)
    {
        var uri = validator.SchemaUri;
        _validators[uri.AbsoluteUri] = validator;
        _registeredHosts.Add(uri.Host);
    }

    /// <inheritdoc />
    public bool TryGetValidator(Uri schemaUri, [NotNullWhen(true)] out ICompiledValidator? validator)
    {
        // Quick rejection: if host isn't registered, no point in looking up
        if (!_registeredHosts.Contains(schemaUri.Host))
        {
            validator = null;
            return false;
        }
        return _validators.TryGetValue(schemaUri.AbsoluteUri, out validator);
    }

    /// <inheritdoc />
    public bool HasValidator(Uri schemaUri)
    {
        if (!_registeredHosts.Contains(schemaUri.Host))
        {
            return false;
        }
        return _validators.ContainsKey(schemaUri.AbsoluteUri);
    }
}
