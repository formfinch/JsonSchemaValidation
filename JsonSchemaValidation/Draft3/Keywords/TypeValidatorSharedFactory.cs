// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft 3 behavior: Creates type validators from type specifications.
// Supports "any" type in addition to standard types.

using FormFinch.JsonSchemaValidation.Abstractions.Keywords;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
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
