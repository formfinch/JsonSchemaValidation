using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Common
{
    public class JsonValidationContextFactory : IJsonValidationContextFactory
    {
        public JsonValidationContextFactory()
        { }

        public JsonValidationContext CreateContextForRoot(JsonElement data)
        {
            return CreateValidationContext(data);
        }

        public JsonValidationContext CreateContextForArrayItem(IJsonValidationContext context, int idx, JsonElement arrayItem)
        {
            return CreateValidationContext(arrayItem);
        }

        public JsonValidationContext CreateContextForProperty(IJsonValidationContext context, string propertyName, JsonElement value)
        {
            return CreateValidationContext(value);
        }

        public JsonValidationContext CopyContext(IJsonValidationContext context)
        {
            var newContext = CreateValidationContext(context.Data);
            if(newContext is IJsonValidationArrayContext target
                && context is IJsonValidationArrayContext source)
            {
                target.SetAnnotations(source.GetAnnotations());
            }
            return newContext;
        }

        private JsonValidationContext CreateValidationContext(JsonElement data)
        {
            if(data.ValueKind == JsonValueKind.Array)
            {
                return new JsonValidationArrayContext(data);
            }

            return new JsonValidationContext(data);
        }
    }
}
