using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PatternValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("pattern", out var patternElement))
            {
                return null;
            }

            if (patternElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? pattern = patternElement.GetString();
            if(string.IsNullOrEmpty(pattern))
            {
                throw new InvalidSchemaException("The value of this pattern must be a string.");
            }

            Regex rxPattern = new(pattern);
            return new PatternValidator(rxPattern);
        }
    }
}
