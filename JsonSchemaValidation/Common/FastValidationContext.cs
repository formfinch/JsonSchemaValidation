using System.Text.Json;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Common
{
    /// <summary>
    /// Lightweight validation context for the IsValid() fast path.
    /// Skips instance location tracking and unevaluated properties initialization.
    /// </summary>
    internal sealed class FastValidationContext : IJsonValidationContext
    {
        private static readonly JsonPointer NoLocation = JsonPointer.Empty;

        private readonly JsonElement _data;
        private readonly IValidationScope _scope;

        public FastValidationContext(JsonElement data, IValidationScope scope)
        {
            _data = data;
            _scope = scope;
        }

        public JsonElement Data => _data;

        public IValidationScope Scope => _scope;

        // Return empty pointer - not tracked in fast path
        public JsonPointer InstanceLocation => NoLocation;
    }
}
