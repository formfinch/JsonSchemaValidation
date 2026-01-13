// Draft 3 behavior: Creates type validators from type specifications.
// Supports "any" type in addition to standard types.

using JsonSchemaValidation.Abstractions.Keywords;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal static class TypeValidatorSharedFactory
    {
        public static IKeywordValidator? CreateFromTypeSpecification(string? typeSpecification)
        {
            return typeSpecification switch
            {
                "any" => TypeAnyValidator.Instance,
                "array" => TypeArrayValidator.Instance,
                "boolean" => TypeBooleanValidator.Instance,
                "integer" => TypeIntegerValidator.Instance,
                "null" => TypeNullValidator.Instance,
                "number" => TypeNumberValidator.Instance,
                "object" => TypeObjectValidator.Instance,
                "string" => TypeStringValidator.Instance,
                _ => null
            };
        }
    }
}
