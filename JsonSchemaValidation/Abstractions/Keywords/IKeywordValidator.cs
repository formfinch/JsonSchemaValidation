using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Abstractions.Keywords
{
    public interface IKeywordValidator
    {
        /// <summary>
        /// The keyword name this validator handles (e.g., "minimum", "properties").
        /// Used for constructing keyword location paths.
        /// </summary>
        string Keyword { get; }

        /// <summary>
        /// Validates the context data against this keyword's constraints.
        /// </summary>
        /// <param name="context">The validation context containing the data to validate.</param>
        /// <param name="keywordLocation">The JSON Pointer to this keyword's location in the schema.</param>
        /// <returns>A validation result with location information.</returns>
        ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation);

        /// <summary>
        /// Fast path validation that returns only a boolean result.
        /// Avoids allocations for ValidationResult, location strings, and children lists.
        /// </summary>
        /// <param name="context">The validation context containing the data to validate.</param>
        /// <returns>True if validation passes, false otherwise.</returns>
        bool IsValid(IJsonValidationContext context)
        {
            // Default implementation delegates to full Validate method.
            // Override in specific validators for better performance.
            return Validate(context, JsonPointer.Empty).IsValid;
        }

        /// <summary>
        /// Ultra-fast path validation for simple validators that only need the JSON data.
        /// Avoids context object allocation entirely.
        /// </summary>
        /// <param name="data">The JSON element to validate.</param>
        /// <returns>True if validation passes, false otherwise.</returns>
        bool IsValid(System.Text.Json.JsonElement data)
        {
            // Default: not supported, will throw if called on complex validator
            throw new System.NotSupportedException(
                $"Validator '{Keyword}' does not support direct JsonElement validation. Use IsValid(IJsonValidationContext) instead.");
        }

        /// <summary>
        /// Indicates whether this validator supports the fast IsValid(JsonElement) overload.
        /// </summary>
        bool SupportsDirectValidation => false;
    }
}
