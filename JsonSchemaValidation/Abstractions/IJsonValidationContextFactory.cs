using System.Text.Json;
using JsonSchemaValidation.Common;

namespace JsonSchemaValidation.Abstractions
{
    public interface IJsonValidationContextFactory
    {
        JsonValidationContext CreateContextForArrayItem(IJsonValidationContext context, int idx, JsonElement arrayItem);
        JsonValidationContext CreateContextForProperty(IJsonValidationContext context, string propertyName, JsonElement value);
        JsonValidationContext CreateContextForRoot(JsonElement data);
        JsonValidationContext CopyContext(IJsonValidationContext context);

        /// <summary>
        /// Creates a fresh context for the same data without copying annotations.
        /// Used by applicators (allOf, anyOf, oneOf, if/then/else) when entering sub-schemas
        /// so that unevaluatedProperties/Items within sub-schemas only see what was
        /// evaluated within that sub-schema, not by sibling applicators.
        /// </summary>
        JsonValidationContext CreateFreshContext(IJsonValidationContext context);
        
        void CopyAnnotations(IJsonValidationContext src, IJsonValidationContext trg);
    }
}
