using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Common
{
    public class JsonValidationContext : IJsonValidationContext
    {
        private readonly JsonElement _data;
        private readonly IValidationScope _scope;
        private readonly JsonPointer _instanceLocation;

        public JsonValidationContext(JsonElement data) : this(data, new ValidationScope(), JsonPointer.Empty)
        {
        }

        public JsonValidationContext(JsonElement data, IValidationScope scope) : this(data, scope, JsonPointer.Empty)
        {
        }

        public JsonValidationContext(JsonElement data, IValidationScope scope, JsonPointer instanceLocation)
        {
            _data = data;
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _instanceLocation = instanceLocation ?? JsonPointer.Empty;
        }

        public JsonElement Data => _data;

        public IValidationScope Scope => _scope;

        public JsonPointer InstanceLocation => _instanceLocation;
    }
}
