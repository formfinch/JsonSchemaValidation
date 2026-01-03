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
            // Root context creates a new scope with empty instance location
            return CreateValidationContext(data, new ValidationScope(), JsonPointer.Empty);
        }

        public JsonValidationContext CreateContextForArrayItem(IJsonValidationContext context, int idx, JsonElement arrayItem)
        {
            // Child contexts share the parent's scope and extend the instance location
            var instanceLocation = context.InstanceLocation.Append(idx);
            return CreateValidationContext(arrayItem, context.Scope, instanceLocation);
        }

        public JsonValidationContext CreateContextForProperty(IJsonValidationContext context, string propertyName, JsonElement value)
        {
            // Child contexts share the parent's scope and extend the instance location
            var instanceLocation = context.InstanceLocation.Append(propertyName);
            return CreateValidationContext(value, context.Scope, instanceLocation);
        }

        public JsonValidationContext CopyContext(IJsonValidationContext context)
        {
            // Copied contexts share the same scope, instance location, and annotations
            var newContext = CreateValidationContext(context.Data, context.Scope, context.InstanceLocation);
            CopyAnnotations(context, newContext);
            return newContext;
        }

        public JsonValidationContext CreateFreshContext(IJsonValidationContext context)
        {
            // Fresh context shares the same scope and instance location but starts with fresh annotations
            // Used by applicators so sub-schemas have independent annotation tracking
            return CreateValidationContext(context.Data, context.Scope, context.InstanceLocation);
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

        private JsonValidationContext CreateValidationContext(JsonElement data, IValidationScope scope, JsonPointer instanceLocation)
        {
            if (data.ValueKind == JsonValueKind.Array)
            {
                return new JsonValidationArrayContext(data, scope, instanceLocation);
            }

            if (data.ValueKind == JsonValueKind.Object)
            {
                return new JsonValidationObjectContext(data, scope, instanceLocation);
            }

            return new JsonValidationContext(data, scope, instanceLocation);
        }
    }
}
