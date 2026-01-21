// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
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
