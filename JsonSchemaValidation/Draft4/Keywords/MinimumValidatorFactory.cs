// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: In Draft 4, exclusiveMinimum is a boolean modifier for minimum.
// Factory for minimum keyword validator with boolean exclusiveMinimum support.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft4.Keywords
{
    internal class MinimumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "minimum";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("minimum", out var minimumElement))
            {
                return null;
            }

            if (!minimumElement.TryGetDouble(out var minimum))
            {
                return null;
            }

            // In Draft 4, exclusiveMinimum is a boolean that modifies minimum behavior
            bool exclusive = false;
            if (schema.TryGetProperty("exclusiveMinimum", out var exclusiveElement) &&
                exclusiveElement.ValueKind == JsonValueKind.True)
            {
                exclusive = true;
            }

            return new MinimumValidator(minimum, exclusive);
        }
    }
}
