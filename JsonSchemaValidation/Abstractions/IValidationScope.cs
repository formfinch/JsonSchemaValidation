// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    /// <summary>
    /// Tracks the schema resources traversed during validation.
    /// Used for dynamic scope resolution ($dynamicRef), error reporting,
    /// and unevaluated property/item tracking.
    /// </summary>
    internal interface IValidationScope
    {
        /// <summary>
        /// Pushes a schema resource onto the scope stack when entering it.
        /// </summary>
        void PushSchemaResource(SchemaMetadata schema);

        /// <summary>
        /// Pops the current schema resource from the scope stack when leaving it.
        /// </summary>
        void PopSchemaResource();

        /// <summary>
        /// Gets the current schema resource (top of stack), or null if empty.
        /// </summary>
        SchemaMetadata? CurrentSchemaResource { get; }

        /// <summary>
        /// Gets the schema resources in dynamic scope, from outermost to innermost.
        /// </summary>
        IEnumerable<SchemaMetadata> GetDynamicScope();

        /// <summary>
        /// Gets the count of schema resources currently in scope.
        /// </summary>
        int Depth { get; }

        /// <summary>
        /// Restores the scope to a previous depth by popping resources.
        /// Used to isolate branch scopes in applicators like if/then/else.
        /// </summary>
        /// <param name="targetDepth">The depth to restore to (must be &lt;= current depth).</param>
        void RestoreToDepth(int targetDepth);

        /// <summary>
        /// Finds the first (outermost) schema resource with HasRecursiveAnchor set to true.
        /// Used for $recursiveRef resolution in Draft 2019-09.
        /// </summary>
        /// <returns>The first schema with recursive anchor, or null if none found.</returns>
        SchemaMetadata? FindFirstRecursiveAnchor();

        /// <summary>
        /// Gets a snapshot of the dynamic scope as an array in LIFO order (innermost first, outermost last).
        /// To iterate outermost-to-innermost (per JSON Schema spec), iterate from Length-1 down to 0.
        /// More efficient than GetDynamicScope() for indexed access patterns.
        /// </summary>
        /// <returns>Array of schema resources with innermost at index 0, outermost at index Length-1.</returns>
        SchemaMetadata[] GetDynamicScopeSnapshot();
    }
}
