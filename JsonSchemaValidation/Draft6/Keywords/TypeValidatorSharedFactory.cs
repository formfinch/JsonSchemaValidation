// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for creating type validators based on type specification string.

using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;

namespace JsonSchemaValidation.Draft6.Keywords
{
    internal static class TypeValidatorSharedFactory
    {
        public static IKeywordValidator? CreateFromTypeSpecification(string? specification)
        {
            return specification switch
            {
                null => null,
                "string" => new TypeStringValidator(),
                "number" => new TypeNumberValidator(),
                "integer" => new TypeIntegerValidator(),
                "boolean" => new TypeBooleanValidator(),
                "array" => new TypeArrayValidator(),
                "object" => new TypeObjectValidator(),
                "null" => new TypeNullValidator(),
                _ => throw new InvalidSchemaException("Unknown type specification")
            };
        }
    }
}
