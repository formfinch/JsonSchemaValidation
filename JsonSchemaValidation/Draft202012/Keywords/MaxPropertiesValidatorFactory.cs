// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class MaxPropertiesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "maxProperties";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("maxProperties", out var maxPropertiesElement))
            {
                return null;
            }

            if (maxPropertiesElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            if (!maxPropertiesElement.TryGetDouble(out var doubleValue))
            {
                return null;
            }

            if (doubleValue < 0 || Math.Abs(doubleValue - Math.Floor(doubleValue)) > double.Epsilon || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'maxProperties' keyword must have a non-negative integer value.");
            }

            return new MaxPropertiesValidator((int)doubleValue);
        }
    }
}
