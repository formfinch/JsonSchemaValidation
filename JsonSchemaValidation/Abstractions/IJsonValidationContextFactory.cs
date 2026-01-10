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

        /// <summary>
        /// Fast path: Creates a lightweight context for property validation without location tracking.
        /// Used by IsValid() path where instance location is not needed.
        /// </summary>
        IJsonValidationContext CreateContextForPropertyFast(IJsonValidationContext context, JsonElement value);

        /// <summary>
        /// Fast path: Creates a lightweight context for array item validation without location tracking.
        /// Used by IsValid() path where instance location is not needed.
        /// </summary>
        IJsonValidationContext CreateContextForArrayItemFast(IJsonValidationContext context, JsonElement arrayItem);

        /// <summary>
        /// Fast path: Creates a fresh context without annotation tracking.
        /// Used by IsValid() path where annotations are not needed.
        /// </summary>
        IJsonValidationContext CreateFreshContextFast(IJsonValidationContext context);

        /// <summary>
        /// Fast path: Creates a fresh context with optional annotation tracking.
        /// When requiresTracking is true, returns a context that implements
        /// IJsonValidationObjectContext/IJsonValidationArrayContext for unevaluated* support.
        /// </summary>
        IJsonValidationContext CreateFreshContextFast(IJsonValidationContext context, bool requiresTracking);

        void CopyAnnotations(IJsonValidationContext src, IJsonValidationContext trg);
    }
}
