using JsonSchemaValidation.Common;
using System.Text.Json;

namespace JsonSchemaValidation.Abstractions
{
    public interface IJsonValidationContextFactory
    {
        JsonValidationContext CreateContextForArrayItem(IJsonValidationContext context, int idx, JsonElement arrayItem);
        JsonValidationContext CreateContextForProperty(IJsonValidationContext context, string propertyName, JsonElement value);
        JsonValidationContext CreateContextForRoot(JsonElement data);
        JsonValidationContext CopyContext(IJsonValidationContext context);

        // todo: should not be in this interface
        void CopyAnnotations(IJsonValidationContext src, IJsonValidationContext trg);
    }
}