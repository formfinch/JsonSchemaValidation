// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: In Draft 3/Draft 4, exclusiveMaximum is a boolean modifier for maximum.
// Factory for maximum keyword validator with boolean exclusiveMaximum support.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal class MaximumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "maximum";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("maximum", out var maximumElement))
            {
                return null;
            }

            if (!maximumElement.TryGetDouble(out var maximum))
            {
                return null;
            }

            // In Draft 3/Draft 4, exclusiveMaximum is a boolean that modifies maximum behavior
            bool exclusive = false;
            if (schema.TryGetProperty("exclusiveMaximum", out var exclusiveElement) &&
                exclusiveElement.ValueKind == JsonValueKind.True)
            {
                exclusive = true;
            }

            return new MaximumValidator(maximum, exclusive);
        }
    }
}
