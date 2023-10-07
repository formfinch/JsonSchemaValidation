using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinLengthValidatorFactory : IKeywordValidatorFactory
    {
        public IKeywordValidator? Create(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("minLength", out var minLengthElement))
            {
                return null;
            }

            if (minLengthElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            if (!minLengthElement.TryGetDouble(out var doubleValue))
            {
                return null;
            }

            if (doubleValue < 0 || doubleValue != Math.Floor(doubleValue) || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException("The 'minLength' keyword must have a non-negative integer value.");
            }

            return new MinLengthValidator((int)doubleValue);
        }
    }
}
