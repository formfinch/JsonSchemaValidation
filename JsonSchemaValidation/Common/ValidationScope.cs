// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Common
{
    /// <summary>
    /// Tracks the schema resources traversed during validation.
    /// Thread-safe implementation using a stack structure.
    /// </summary>
    public class ValidationScope : IValidationScope
    {
        private readonly Stack<SchemaMetadata> _schemaStack = new();

        public void PushSchemaResource(SchemaMetadata schema)
        {
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            _schemaStack.Push(schema);
        }

        public void PopSchemaResource()
        {
            if (_schemaStack.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop from empty validation scope.");
            }
            _schemaStack.Pop();
        }

        public SchemaMetadata? CurrentSchemaResource =>
            _schemaStack.Count > 0 ? _schemaStack.Peek() : null;

        public IEnumerable<SchemaMetadata> GetDynamicScope()
        {
            // Return from outermost (bottom of stack) to innermost (top of stack)
            return _schemaStack.Reverse();
        }

        public int Depth => _schemaStack.Count;

        /// <summary>
        /// Finds the first (outermost) schema resource with HasRecursiveAnchor set to true.
        /// Returns null if no such schema is found in the dynamic scope.
        /// </summary>
        public SchemaMetadata? FindFirstRecursiveAnchor()
        {
            // Stack.ToArray() returns items in LIFO order (top first, bottom last)
            // We need to iterate from outermost (bottom = last in array) to innermost (top = first in array)
            var items = _schemaStack.ToArray();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                if (items[i].HasRecursiveAnchor)
                {
                    return items[i];
                }
            }
            return null;
        }

        public void RestoreToDepth(int targetDepth)
        {
            if (targetDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetDepth), "Target depth cannot be negative.");
            }

            if (targetDepth > _schemaStack.Count)
            {
                ThrowDepthExceedsException(targetDepth, _schemaStack.Count);
            }

            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            static void ThrowDepthExceedsException(int targetDepth, int currentDepth)
            {
                throw new ArgumentOutOfRangeException(nameof(targetDepth),
                    $"Target depth {targetDepth.ToString(System.Globalization.CultureInfo.InvariantCulture)} exceeds current depth {currentDepth.ToString(System.Globalization.CultureInfo.InvariantCulture)}.");
            }

            while (_schemaStack.Count > targetDepth)
            {
                _schemaStack.Pop();
            }
        }
    }
}
