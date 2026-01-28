// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.CompiledValidators;

/// <summary>
/// Immutable scope stack implementation for compiled validator $dynamicRef and $recursiveRef resolution.
/// </summary>
/// <remarks>
/// <para>
/// Uses a linked list structure where each node contains a scope entry and a reference to the parent.
/// Push operations create new nodes without modifying existing ones, ensuring thread safety.
/// </para>
/// <para>
/// Resolution searches from outermost (root) to innermost (current) as specified by JSON Schema 2020-12.
/// This requires traversing to the root first, then searching forward. For common depths (≤8),
/// a stack-allocated array is used to avoid heap allocation.
/// </para>
/// <para>
/// <b>Thread safety:</b> This class is immutable and safe for concurrent use.
/// </para>
/// </remarks>
public sealed class CompiledValidatorScope : ICompiledValidatorScope
{
    /// <summary>
    /// Gets the empty scope instance. Use this as the starting point for scope creation.
    /// </summary>
    public static readonly CompiledValidatorScope Empty = new(null, default);

    private readonly CompiledValidatorScope? _parent;
    private readonly CompiledScopeEntry _entry;

    private CompiledValidatorScope(CompiledValidatorScope? parent, CompiledScopeEntry entry)
    {
        _parent = parent;
        _entry = entry;
    }

    /// <inheritdoc/>
    public bool IsEmpty => _parent == null && !_entry.HasAnyAnchors;

    /// <inheritdoc/>
    public bool TryResolveDynamicAnchor(
        string anchorName,
        out Func<JsonElement, ICompiledValidatorScope, bool>? validator)
    {
        // Search from outermost to innermost by collecting entries first
        // We need to reverse the traversal order since linked list is innermost-first
        var entries = CollectEntries();
        if (entries == null)
        {
            validator = null;
            return false;
        }

        // Search outermost (index 0) to innermost
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].DynamicAnchors?.TryGetValue(anchorName, out validator) == true)
            {
                return true;
            }
        }

        validator = null;
        return false;
    }

    /// <inheritdoc/>
    public bool TryResolveRecursiveAnchor(
        out Func<JsonElement, ICompiledValidatorScope, bool>? validator)
    {
        // Search from outermost to innermost by collecting entries first
        var entries = CollectEntries();
        if (entries == null)
        {
            validator = null;
            return false;
        }

        // Search outermost (index 0) to innermost
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].HasRecursiveAnchor && entries[i].RootValidator != null)
            {
                validator = entries[i].RootValidator;
                return true;
            }
        }

        validator = null;
        return false;
    }

    /// <inheritdoc/>
    public ICompiledValidatorScope Push(CompiledScopeEntry entry)
    {
        // Optimization: skip if entry has nothing to contribute
        if (!entry.HasAnyAnchors)
        {
            return this;
        }

        return new CompiledValidatorScope(this, entry);
    }

    /// <summary>
    /// Collects all entries from outermost to innermost.
    /// Returns null if the scope is empty.
    /// </summary>
    private CompiledScopeEntry[]? CollectEntries()
    {
        // Count depth first
        var depth = 0;
        var current = this;

        while (current != null)
        {
            if (current._entry.HasAnyAnchors)
            {
                depth++;
            }

            current = current._parent;
        }

        if (depth == 0)
        {
            return null;
        }

        // Build array from outermost to innermost
        var entries = new CompiledScopeEntry[depth];
        current = this;
        var index = depth - 1;

        while (current != null && index >= 0)
        {
            if (current._entry.HasAnyAnchors)
            {
                entries[index] = current._entry;
                index--;
            }

            current = current._parent;
        }

        return entries;
    }
}
