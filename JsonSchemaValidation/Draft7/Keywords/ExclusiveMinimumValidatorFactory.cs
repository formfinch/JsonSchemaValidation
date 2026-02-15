// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 2019-09, Draft 2020-12 (numeric value)
// Note: In Draft 4-7, exclusiveMinimum was a boolean modifier for minimum.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
{
    internal class ExclusiveMinimumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "exclusiveMinimum";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("exclusiveMinimum", out var exclusiveMinimumElement))
            {
                return null;
            }

            if (!exclusiveMinimumElement.TryGetDouble(out var minimum))
            {
                return null;
            }

            return new ExclusiveMinimumValidator(minimum);
        }
    }
}
