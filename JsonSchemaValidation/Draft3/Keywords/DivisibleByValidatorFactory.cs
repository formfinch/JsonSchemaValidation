// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft 3 behavior: divisibleBy is equivalent to multipleOf in later drafts.
// Factory for divisibleBy keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal class DivisibleByValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "divisibleBy";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("divisibleBy", out var divisibleByElement))
            {
                return null;
            }

            if (!divisibleByElement.TryGetDouble(out var divisor))
            {
                return null;
            }

            if (divisor <= 0)
            {
                throw new InvalidSchemaException("The 'divisibleBy' keyword must have a number value greater than 0.");
            }

            return new DivisibleByValidator(divisor);
        }
    }
}
