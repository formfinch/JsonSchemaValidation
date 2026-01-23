// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.CompiledValidators;

/// <summary>
/// Registry for compiled validators indexed by schema URI or content hash.
/// </summary>
/// <remarks>
/// <b>Thread safety:</b> Implementations should be safe for concurrent registration and lookup.
/// </remarks>
public interface ICompiledValidatorRegistry
{
    /// <summary>
    /// Registers a compiled validator for its schema URI.
    /// If a validator is already registered for the URI, it will be replaced.
    /// </summary>
    /// <param name="validator">The compiled validator to register.</param>
    void Register(ICompiledValidator validator);

    /// <summary>
    /// Registers a compiled validator by its content hash.
    /// This allows lookup of validators for schemas without stable $id.
    /// </summary>
    /// <param name="contentHash">The content hash of the schema.</param>
    /// <param name="validator">The compiled validator to register.</param>
    void RegisterByHash(string contentHash, ICompiledValidator validator);

    /// <summary>
    /// Attempts to get a compiled validator for the given schema URI.
    /// </summary>
    /// <param name="schemaUri">The schema URI to look up.</param>
    /// <param name="validator">The compiled validator if found; otherwise, null.</param>
    /// <returns>True if a compiled validator was found; otherwise, false.</returns>
    bool TryGetValidator(Uri schemaUri, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ICompiledValidator? validator);

    /// <summary>
    /// Attempts to get a compiled validator by content hash.
    /// </summary>
    /// <param name="contentHash">The content hash of the schema.</param>
    /// <param name="validator">The compiled validator if found; otherwise, null.</param>
    /// <returns>True if a compiled validator was found; otherwise, false.</returns>
    bool TryGetValidatorByHash(string contentHash, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ICompiledValidator? validator);

    /// <summary>
    /// Checks if a compiled validator exists for the given schema URI.
    /// </summary>
    /// <param name="schemaUri">The schema URI to check.</param>
    /// <returns>True if a compiled validator is registered; otherwise, false.</returns>
    bool HasValidator(Uri schemaUri);

    /// <summary>
    /// Checks if a compiled validator exists for the given content hash.
    /// </summary>
    /// <param name="contentHash">The content hash of the schema.</param>
    /// <returns>True if a compiled validator is registered; otherwise, false.</returns>
    bool HasValidatorByHash(string contentHash);

    /// <summary>
    /// Registers a compiled validator for a specific URI (which may include a fragment).
    /// Use this to register subschema validators by their full URI with JSON Pointer fragment.
    /// </summary>
    /// <param name="schemaUri">The URI to register the validator under (may include fragment).</param>
    /// <param name="validator">The compiled validator to register.</param>
    void RegisterForUri(Uri schemaUri, ICompiledValidator validator);
}
