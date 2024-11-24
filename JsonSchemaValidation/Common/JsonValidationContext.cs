using System.Text.Json;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Common
{
    public class JsonValidationContext : IJsonValidationContext
    {
        private readonly JsonElement _data;

        public JsonValidationContext(JsonElement data)
        {
            _data = data;
        }

        public JsonElement Data
        {
            get { return _data; }
        }
    }
}
