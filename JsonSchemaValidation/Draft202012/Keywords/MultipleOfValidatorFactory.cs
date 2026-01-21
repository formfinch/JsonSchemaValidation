// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class MultipleOfValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "multipleOf";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("multipleOf", out var multipleOfElement))
            {
                return null;
            }

            if (!multipleOfElement.TryGetDouble(out var divisor))
            {
                return null;
            }

            if (divisor <= 0)
            {
                throw new InvalidSchemaException("The 'multipleOf' keyword must have a number value greater than 0.");
            }

            return new MultipleOfValidator(divisor);
        }
    }
}
