using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Keywords;
using JsonSchemaValidation.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class UniqueItemsValidatorFactory : IKeywordValidatorFactory
    {
        public IKeywordValidator? Create(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("uniqueItems", out var uniqueItemsElement))
            {
                return null;
            }

            if (uniqueItemsElement.ValueKind != JsonValueKind.True
                && uniqueItemsElement.ValueKind != JsonValueKind.False)
            {
                throw new InvalidSchemaException("The 'uniqueItems' keyword must have a boolean value.");
            }

            if(uniqueItemsElement.ValueKind == JsonValueKind.False)
            {
                return null;
            }

            return new UniqueItemsValidator();
        }
    }
}
