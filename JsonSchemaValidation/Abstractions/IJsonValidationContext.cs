using System.Text.Json;

namespace JsonSchemaValidation.Abstractions
{
    public interface IJsonValidationContext
    {
        JsonElement Data { get; }

        /// <summary>
        /// The validation scope tracking schema resources traversed during validation.
        /// Used for dynamic scope resolution, error reporting, and unevaluated tracking.
        /// </summary>
        IValidationScope Scope { get; }
    }
}