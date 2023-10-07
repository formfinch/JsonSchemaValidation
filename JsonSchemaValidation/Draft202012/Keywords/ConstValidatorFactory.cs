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
    internal class ConstValidatorFactory : IKeywordValidatorFactory
    {
        public IKeywordValidator? Create(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("const", out var constElement))
            {
                return null;
            }

            if(constElement.ValueKind == JsonValueKind.Undefined)
            {
                throw new InvalidSchemaException("const cannot be undefined.");
            }

            return new ConstValidator(constElement);
        }
    }
}
