using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.CompiledValidators;

/// <summary>
/// Thread-safe registry for compiled validators indexed by schema URI.
/// </summary>
public sealed class CompiledValidatorRegistry : ICompiledValidatorRegistry
{
    private readonly ConcurrentDictionary<Uri, ICompiledValidator> _validators = new();

    /// <inheritdoc />
    public void Register(ICompiledValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validators[validator.SchemaUri] = validator;
    }

    /// <inheritdoc />
    public bool TryGetValidator(Uri schemaUri, [NotNullWhen(true)] out ICompiledValidator? validator)
    {
        ArgumentNullException.ThrowIfNull(schemaUri);
        return _validators.TryGetValue(schemaUri, out validator);
    }

    /// <inheritdoc />
    public bool HasValidator(Uri schemaUri)
    {
        ArgumentNullException.ThrowIfNull(schemaUri);
        return _validators.ContainsKey(schemaUri);
    }
}
