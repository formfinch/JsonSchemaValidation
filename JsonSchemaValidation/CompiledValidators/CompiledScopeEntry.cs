// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.CompiledValidators;

/// <summary>
/// Represents an entry in the compiled validator scope stack.
/// </summary>
/// <remarks>
/// <para>
/// Each entry corresponds to a schema resource that declares either $dynamicAnchor(s)
/// or $recursiveAnchor: true. Schemas without these declarations do not need scope entries.
/// </para>
/// <para>
/// The entry contains validation functions rather than validator instances to avoid
/// interface dispatch overhead in the hot path.
/// </para>
/// </remarks>
public readonly struct CompiledScopeEntry
{
    /// <summary>
    /// Gets the dynamic anchors declared at this scope level.
    /// </summary>
    /// <remarks>
    /// Key: anchor name (without the # prefix).
    /// Value: validation function that accepts (element, scope) and returns validity.
    /// Null if no dynamic anchors are declared at this scope level.
    /// </remarks>
    public IReadOnlyDictionary<string, Func<JsonElement, ICompiledValidatorScope, bool>>? DynamicAnchors { get; init; }

    /// <summary>
    /// Gets the root validation function for this scope level.
    /// </summary>
    /// <remarks>
    /// Used for $recursiveRef resolution. When $recursiveRef is evaluated and this scope
    /// has <see cref="HasRecursiveAnchor"/> set to true, this function is invoked.
    /// Null if <see cref="HasRecursiveAnchor"/> is false.
    /// </remarks>
    public Func<JsonElement, ICompiledValidatorScope, bool>? RootValidator { get; init; }

    /// <summary>
    /// Gets a value indicating whether this scope declares $recursiveAnchor: true.
    /// </summary>
    /// <remarks>
    /// This is only relevant for Draft 2019-09 schemas. In Draft 2020-12, $recursiveRef
    /// was replaced by $dynamicRef.
    /// </remarks>
    public bool HasRecursiveAnchor { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entry contributes to scope resolution.
    /// </summary>
    /// <remarks>
    /// Returns true if the entry has any dynamic anchors or recursive anchor.
    /// Entries with no anchors should not be pushed onto the scope stack.
    /// </remarks>
    public bool HasAnyAnchors => DynamicAnchors?.Count > 0 || HasRecursiveAnchor;
}
