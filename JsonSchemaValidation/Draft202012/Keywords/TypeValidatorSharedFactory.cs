using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal static class TypeValidatorSharedFactory
    {
        public static IKeywordValidator? CreateFromTypeSpecification(string? specification)
        {
            if (specification == null)
            {
                return null;
            }

            if (specification == "string")
            {
                return new TypeStringValidator();
            }

            if (specification == "number")
            {
                return new TypeNumberValidator();
            }

            if (specification == "integer")
            {
                return new TypeIntegerValidator();
            }

            if (specification == "boolean")
            {
                return new TypeBooleanValidator();
            }

            if (specification == "array")
            {
                return new TypeArrayValidator();
            }

            if (specification == "object")
            {
                return new TypeObjectValidator();
            }

            if (specification == "null")
            {
                return new TypeNullValidator();
            }

            throw new InvalidSchemaException("Unknown type specification");
        }
    }
}
