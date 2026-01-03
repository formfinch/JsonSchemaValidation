using System.Text.Json;
using JsonSchemaValidation.Common;

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

        /// <summary>
        /// JSON Pointer to the current instance location being validated.
        /// For example: "/users/0/email" for the email property of the first user.
        /// </summary>
        JsonPointer InstanceLocation { get; }
    }
}