using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaValidator
    {
        void AddKeywordValidator(IKeywordValidator keywordValidator);

        /// <summary>
        /// Validates the context data against all keyword validators in this schema.
        /// </summary>
        /// <param name="context">The validation context containing the data to validate.</param>
        /// <param name="keywordLocation">The JSON Pointer to this schema's location. Empty for root schema.</param>
        /// <returns>An aggregated validation result from all keyword validators.</returns>
        ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation);

        /// <summary>
        /// Fast path validation that returns only a boolean result.
        /// Short-circuits on first failure and avoids building the full result tree.
        /// </summary>
        /// <param name="context">The validation context containing the data to validate.</param>
        /// <returns>True if all keyword validators pass, false otherwise.</returns>
        bool IsValid(IJsonValidationContext context);
    }
}
