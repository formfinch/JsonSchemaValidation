using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Validation;
using FormFinch.JsonSchemaValidation.Validation.Output;

namespace FormFinch.JsonSchemaValidation.Common
{
    /// <summary>
    /// Extension methods for ISchemaValidator providing convenience APIs.
    /// </summary>
    public static class SchemaValidatorExtensions
    {
        /// <summary>
        /// Validates the context data against the schema at the root level.
        /// Equivalent to calling Validate(context, JsonPointer.Empty).
        /// </summary>
        public static ValidationResult ValidateRoot(this ISchemaValidator validator, IJsonValidationContext context)
        {
            return validator.Validate(context, JsonPointer.Empty);
        }

        /// <summary>
        /// Fast path validation that returns only a boolean result.
        /// Short-circuits on first failure and avoids building the full result tree.
        /// Use this when you only need to know if the data is valid, not why it's invalid.
        /// </summary>
        public static bool IsValidRoot(this ISchemaValidator validator, IJsonValidationContext context)
        {
            return validator.IsValid(context);
        }

        /// <summary>
        /// Validates and returns the result in the specified output format.
        /// </summary>
        public static OutputUnit ValidateWithOutput(
            this ISchemaValidator validator,
            IJsonValidationContext context,
            OutputFormat format)
        {
            var result = validator.Validate(context, JsonPointer.Empty);
            return result.ToOutputUnit(format);
        }

        /// <summary>
        /// Validates and returns the result in Flag format (just valid/invalid).
        /// </summary>
        public static OutputUnit ValidateFlag(this ISchemaValidator validator, IJsonValidationContext context)
        {
            return validator.ValidateWithOutput(context, OutputFormat.Flag);
        }

        /// <summary>
        /// Validates and returns the result in Basic format (flat error list).
        /// </summary>
        public static OutputUnit ValidateBasic(this ISchemaValidator validator, IJsonValidationContext context)
        {
            return validator.ValidateWithOutput(context, OutputFormat.Basic);
        }

        /// <summary>
        /// Validates and returns the result in Detailed format (hierarchical output).
        /// </summary>
        public static OutputUnit ValidateDetailed(this ISchemaValidator validator, IJsonValidationContext context)
        {
            return validator.ValidateWithOutput(context, OutputFormat.Detailed);
        }
    }
}
