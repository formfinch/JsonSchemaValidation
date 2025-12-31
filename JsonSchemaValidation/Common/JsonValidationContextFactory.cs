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
            // Root context creates a new scope
            return CreateValidationContext(data, new ValidationScope());
        }

        public JsonValidationContext CreateContextForArrayItem(IJsonValidationContext context, int idx, JsonElement arrayItem)
        {
            // Child contexts share the parent's scope
            return CreateValidationContext(arrayItem, context.Scope);
        }

        public JsonValidationContext CreateContextForProperty(IJsonValidationContext context, string propertyName, JsonElement value)
        {
            // Child contexts share the parent's scope
            return CreateValidationContext(value, context.Scope);
        }

        public JsonValidationContext CopyContext(IJsonValidationContext context)
        {
            // Copied contexts share the same scope
            var newContext = CreateValidationContext(context.Data, context.Scope);
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

        private JsonValidationContext CreateValidationContext(JsonElement data, IValidationScope scope)
        {
            if (data.ValueKind == JsonValueKind.Array)
            {
                return new JsonValidationArrayContext(data, scope);
            }

            if (data.ValueKind == JsonValueKind.Object)
            {
                return new JsonValidationObjectContext(data, scope);
            }

            return new JsonValidationContext(data, scope);
        }
    }
}
