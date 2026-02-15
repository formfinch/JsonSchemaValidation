// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for maxLength keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
{
    internal class MaxLengthValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "maxLength";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("maxLength", out var maxLengthElement))
            {
                return null;
            }

            if (maxLengthElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            if (!maxLengthElement.TryGetDouble(out var doubleValue))
            {
                return null;
            }

            if (doubleValue < 0 || Math.Abs(doubleValue - Math.Floor(doubleValue)) > double.Epsilon || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'maxLength' keyword must have a non-negative integer value.");
            }

            return new MaxLengthValidator((int)doubleValue);
        }
    }
}
