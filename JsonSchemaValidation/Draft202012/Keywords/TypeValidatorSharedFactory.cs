using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeValidatorSharedFactory
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
