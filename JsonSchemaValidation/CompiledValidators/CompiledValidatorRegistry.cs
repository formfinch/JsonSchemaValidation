// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.CompiledValidators;

/// <summary>
/// Thread-safe registry for compiled validators indexed by schema URI or content hash.
/// Uses string keys for faster lookups (Uri equality is slow).
/// </summary>
public sealed class CompiledValidatorRegistry : ICompiledValidatorRegistry
{
    // Use ConcurrentDictionary for thread-safe reads and writes.
    // While typically populated once during initialization, using concurrent collections
    // ensures safety if registration occurs after startup.
    private readonly ConcurrentDictionary<string, ICompiledValidator> _validatorsByUri = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ICompiledValidator> _validatorsByHash = new(StringComparer.Ordinal);

    // Track registered hosts for quick rejection of non-matching URIs
    // ConcurrentDictionary used as a thread-safe set (values are ignored)
    private readonly ConcurrentDictionary<string, byte> _registeredHosts = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Register(ICompiledValidator validator)
    {
        var uri = validator.SchemaUri;
        _validatorsByUri[uri.AbsoluteUri] = validator;
        _registeredHosts.TryAdd(uri.Host, 0);
    }

    /// <inheritdoc />
    public void RegisterByHash(string contentHash, ICompiledValidator validator)
    {
        _validatorsByHash[contentHash] = validator;
    }

    /// <inheritdoc />
    public bool TryGetValidator(Uri schemaUri, [NotNullWhen(true)] out ICompiledValidator? validator)
    {
        // Quick rejection: if host isn't registered, no point in looking up
        if (!_registeredHosts.ContainsKey(schemaUri.Host))
        {
            validator = null;
            return false;
        }
        return _validatorsByUri.TryGetValue(schemaUri.AbsoluteUri, out validator);
    }

    /// <inheritdoc />
    public bool TryGetValidatorByHash(string contentHash, [NotNullWhen(true)] out ICompiledValidator? validator)
    {
        return _validatorsByHash.TryGetValue(contentHash, out validator);
    }

    /// <inheritdoc />
    public bool HasValidator(Uri schemaUri)
    {
        if (!_registeredHosts.ContainsKey(schemaUri.Host))
        {
            return false;
        }
        return _validatorsByUri.ContainsKey(schemaUri.AbsoluteUri);
    }

    /// <inheritdoc />
    public bool HasValidatorByHash(string contentHash)
    {
        return _validatorsByHash.ContainsKey(contentHash);
    }

    /// <inheritdoc />
    public void RegisterForUri(Uri schemaUri, ICompiledValidator validator)
    {
        _validatorsByUri[schemaUri.AbsoluteUri] = validator;
        _registeredHosts.TryAdd(schemaUri.Host, 0);
    }
}
