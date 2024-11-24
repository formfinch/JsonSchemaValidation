using JsonSchemaValidation.Abstractions;
using System.Text.Json;

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
            CopyAnnotations(context, newContext);
            return newContext;
        }

        public void CopyAnnotations(IJsonValidationContext src, IJsonValidationContext trg)
        {
            if (trg is IJsonValidationArrayContext targetArrayContext
                && src is IJsonValidationArrayContext sourceArrayContext)
            {
                targetArrayContext.SetAnnotations(sourceArrayContext.GetAnnotations());
            }

            if (trg is IJsonValidationObjectContext targetObjectContext
                && src is IJsonValidationObjectContext sourceObjectContext)
            {
                targetObjectContext.SetAnnotations(sourceObjectContext.GetAnnotations());
            }
        }

        private JsonValidationContext CreateValidationContext(JsonElement data)
        {
            if(data.ValueKind == JsonValueKind.Array)
            {
                return new JsonValidationArrayContext(data);
            }

            if (data.ValueKind == JsonValueKind.Object)
            {
                return new JsonValidationObjectContext(data);
            }

            return new JsonValidationContext(data);
        }
    }
}
