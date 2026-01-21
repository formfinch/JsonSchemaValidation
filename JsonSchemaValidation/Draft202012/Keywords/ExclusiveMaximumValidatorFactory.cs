// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class ExclusiveMaximumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "exclusiveMaximum";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("exclusiveMaximum", out var exclusiveMaximumElement))
            {
                return null;
            }

            if (!exclusiveMaximumElement.TryGetDouble(out var maximum))
            {
                return null;
            }

            return new ExclusiveMaximumValidator(maximum);
        }
    }
}
