using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Draft202012.Keywords;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ExclusiveMaximumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public IKeywordValidator? Create(JsonElement schema)
        {
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
