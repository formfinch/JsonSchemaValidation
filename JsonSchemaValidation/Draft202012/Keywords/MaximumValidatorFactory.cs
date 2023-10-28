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
    internal class MaximumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public IKeywordValidator? Create(JsonElement schema)
        {
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
