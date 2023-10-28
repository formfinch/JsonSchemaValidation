using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MultipleOfValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public IKeywordValidator? Create(JsonElement schema)
        {
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
