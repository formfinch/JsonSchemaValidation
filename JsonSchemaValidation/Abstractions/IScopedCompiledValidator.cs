// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.CompiledValidators;

namespace FormFinch.JsonSchemaValidation.Abstractions;

/// <summary>
/// A compiled validator that supports dynamic scope tracking for $dynamicRef and $recursiveRef resolution.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends <see cref="ICompiledValidator"/> with scope-aware validation.
/// All FormFinch-generated compiled validators implement this interface to support
/// proper dynamic reference resolution.
/// </para>
/// <para>
/// The base <see cref="ICompiledValidator.IsValid(JsonElement)"/> method creates an initial
/// scope containing this validator's anchors and delegates to the scoped validation method.
/// For nested validation through $ref chains, the scoped method should be called directly
/// to propagate the scope stack.
/// </para>
/// <para>
/// <b>Thread safety:</b> After initialization, implementations must be safe for concurrent
/// calls to both <see cref="ICompiledValidator.IsValid(JsonElement)"/> and
/// <see cref="IsValid(JsonElement, ICompiledValidatorScope)"/>.
/// </para>
/// </remarks>
public interface IScopedCompiledValidator : ICompiledValidator
{
    /// <summary>
    /// Gets the dynamic anchors declared by this validator's root schema resource.
    /// </summary>
    /// <remarks>
    /// Key: anchor name (without the # prefix).
    /// Value: validation function for the anchored subschema that accepts (element, scope, location).
    /// Returns null if no dynamic anchors are declared.
    /// </remarks>
    IReadOnlyDictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>? DynamicAnchors { get; }

    /// <summary>
    /// Gets a value indicating whether this validator's root schema has $recursiveAnchor: true.
    /// </summary>
    /// <remarks>
    /// This is only relevant for Draft 2019-09 schemas. In Draft 2020-12, $recursiveRef
    /// was replaced by $dynamicRef.
    /// </remarks>
    bool HasRecursiveAnchor { get; }

    /// <summary>
    /// Gets the root validation function for $recursiveRef resolution.
    /// </summary>
    /// <remarks>
    /// The function accepts (element, scope, location) and returns validity.
    /// Returns null if <see cref="HasRecursiveAnchor"/> is false.
    /// </remarks>
    Func<JsonElement, ICompiledValidatorScope, string, bool>? RootValidator { get; }

    /// <summary>
    /// Validates the JSON instance with dynamic scope tracking.
    /// </summary>
    /// <param name="instance">The JSON element to validate.</param>
    /// <param name="scope">The current dynamic scope stack.</param>
    /// <returns>True if the instance is valid according to the schema; otherwise, false.</returns>
    /// <remarks>
    /// This method should be called when validating through $ref chains to maintain
    /// the dynamic scope stack. The scope enables proper resolution of $dynamicRef
    /// and $recursiveRef keywords.
    /// </remarks>
    bool IsValid(JsonElement instance, ICompiledValidatorScope scope);
}
