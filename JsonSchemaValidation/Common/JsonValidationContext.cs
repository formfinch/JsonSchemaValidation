using System.Text.Json;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Common
{
    public class JsonValidationContext : IJsonValidationContext
    {
        private readonly JsonElement _data;
        private readonly IValidationScope _scope;

        public JsonValidationContext(JsonElement data) : this(data, new ValidationScope())
        {
        }

        public JsonValidationContext(JsonElement data, IValidationScope scope)
        {
            _data = data;
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public JsonElement Data => _data;

        public IValidationScope Scope => _scope;
    }
}
