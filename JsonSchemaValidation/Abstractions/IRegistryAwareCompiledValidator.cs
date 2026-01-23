// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CompiledValidators;

namespace FormFinch.JsonSchemaValidation.Abstractions;

/// <summary>
/// A compiled validator that can resolve external $ref dependencies from a registry.
/// Extends <see cref="ICompiledValidator"/> with initialization support for external references.
/// </summary>
/// <remarks>
/// Validators implementing this interface should have their initialization methods called in order:
/// 1. <see cref="RegisterSubschemas"/> - registers any subschemas (e.g., $defs) this validator provides
/// 2. <see cref="Initialize"/> - resolves external $ref dependencies from the registry
/// This two-phase initialization allows validators to depend on subschemas from other validators.
/// <para>
/// <b>Thread safety:</b> Initialization must be performed once, before any concurrent validation.
/// After initialization, implementations must be safe for concurrent calls to <see cref="ICompiledValidator.IsValid(JsonElement)"/>.
/// </para>
/// </remarks>
public interface IRegistryAwareCompiledValidator : ICompiledValidator
{
    /// <summary>
    /// Registers subschemas from this validator into the registry.
    /// This includes subschemas under $defs that other validators may reference via fragment URIs.
    /// This method should be called BEFORE <see cref="Initialize"/> is called on any validator.
    /// </summary>
    /// <param name="registry">The registry to register subschemas into.</param>
    void RegisterSubschemas(ICompiledValidatorRegistry registry);

    /// <summary>
    /// Resolves all external $ref dependencies from the registry.
    /// This method must be called once before the validator is used.
    /// </summary>
    /// <param name="registry">The registry containing compiled validators for external schemas.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any external $ref cannot be resolved from the registry.
    /// </exception>
    void Initialize(ICompiledValidatorRegistry registry);

    /// <summary>
    /// Sets the dynamic scope root for $dynamicRef resolution.
    /// When this validator is used as part of a larger schema (e.g., via allOf),
    /// $dynamicRef should resolve to the outermost $dynamicAnchor, not the local one.
    /// </summary>
    /// <param name="root">The validator that should be used for $dynamicRef resolution.
    /// Pass null to use local resolution (the default).</param>
    void SetDynamicScopeRoot(ICompiledValidator? root);
}
