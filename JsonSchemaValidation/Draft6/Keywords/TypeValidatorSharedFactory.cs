// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for creating type validators based on type specification string.

using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
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
