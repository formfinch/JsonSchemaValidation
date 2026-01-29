// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.CompiledValidators;

namespace FormFinch.JsonSchemaValidation.Abstractions;

/// <summary>
/// Dynamic scope stack for compiled validator $dynamicRef and $recursiveRef resolution.
/// </summary>
/// <remarks>
/// <para>
/// This interface represents an immutable scope stack that tracks schema resources during validation.
/// Each push operation returns a new instance, preserving the original scope.
/// </para>
/// <para>
/// Resolution order follows the JSON Schema specification: searches from outermost (first encountered
/// during evaluation) to innermost (most recent). This is specified in JSON Schema 2020-12 section 8.2.3.2:
/// "the outermost schema resource in the dynamic scope that defines an identically named fragment".
/// </para>
/// <para>
/// <b>Thread safety:</b> Implementations must be immutable and safe for concurrent use.
/// A new scope instance should be created per validation invocation.
/// </para>
/// </remarks>
public interface ICompiledValidatorScope
{
    /// <summary>
    /// Searches the scope stack from outermost to innermost for a matching $dynamicAnchor.
    /// </summary>
    /// <param name="anchorName">The anchor name to search for (without the # prefix).</param>
    /// <param name="validator">
    /// When this method returns true, contains the validation function for the matched anchor.
    /// The function accepts (element, scope, location) and returns validity.
    /// When this method returns false, contains null.
    /// </param>
    /// <returns>
    /// True if a matching $dynamicAnchor was found in the dynamic scope;
    /// false to indicate the caller should use the static (bookend) target.
    /// </returns>
    bool TryResolveDynamicAnchor(
        string anchorName,
        out Func<JsonElement, ICompiledValidatorScope, string, bool>? validator);

    /// <summary>
    /// Searches the scope stack from outermost to innermost for a schema with $recursiveAnchor: true.
    /// </summary>
    /// <param name="validator">
    /// When this method returns true, contains the root validation function for the matched schema.
    /// The function accepts (element, scope, location) and returns validity.
    /// When this method returns false, contains null.
    /// </param>
    /// <returns>
    /// True if a schema with $recursiveAnchor: true was found in the dynamic scope;
    /// false to indicate the caller should use the static target.
    /// </returns>
    bool TryResolveRecursiveAnchor(
        out Func<JsonElement, ICompiledValidatorScope, string, bool>? validator);

    /// <summary>
    /// Creates a new scope with the specified entry pushed onto the stack.
    /// </summary>
    /// <param name="entry">The scope entry to push.</param>
    /// <returns>
    /// A new scope instance with the entry added if the entry has anchors;
    /// otherwise, returns this instance unchanged (optimization for entries with no anchors).
    /// </returns>
    ICompiledValidatorScope Push(CompiledScopeEntry entry);

    /// <summary>
    /// Gets a value indicating whether the scope stack is empty.
    /// </summary>
    bool IsEmpty { get; }
}
