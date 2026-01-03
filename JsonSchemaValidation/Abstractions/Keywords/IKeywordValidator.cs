using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Abstractions.Keywords
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
    }
}
