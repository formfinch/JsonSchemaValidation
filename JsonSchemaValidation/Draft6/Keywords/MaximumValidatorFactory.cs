// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for maximum keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
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

            return new MaximumValidator(maximum);
        }
    }
}
