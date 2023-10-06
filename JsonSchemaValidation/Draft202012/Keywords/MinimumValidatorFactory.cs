using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Keywords;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class MinimumValidatorFactory : IKeywordValidatorFactory
    {
        public IKeywordValidator? Create(JsonElement schema)
        {
            if(schema.ValueKind == JsonValueKind.Object
                && schema.TryGetProperty("minimum", out var minimumElement)
                && minimumElement.TryGetDouble(out var minimum))
            {
                return new MinimumValidator(minimum);
            }

            return null;
        }
    }
}
