using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Common
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

        public void RestoreToDepth(int targetDepth)
        {
            if (targetDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetDepth), "Target depth cannot be negative.");
            }

            if (targetDepth > _schemaStack.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(targetDepth),
                    $"Target depth {targetDepth} exceeds current depth {_schemaStack.Count}.");
            }

            while (_schemaStack.Count > targetDepth)
            {
                _schemaStack.Pop();
            }
        }
    }
}
