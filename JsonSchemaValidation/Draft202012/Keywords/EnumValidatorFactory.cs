using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class EnumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private static readonly JsonElementComparison _comparison = new();

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("enum", out var enumValuesElement))
            {
                return null;
            }

            if(enumValuesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidSchemaException("The value of the enum keyword must be an array. ");
            }

            int nEnumValues = enumValuesElement.GetArrayLength();
            if (nEnumValues == 0)
            {
                throw new InvalidSchemaException("The enum array should have at least one element");
            }

            for (int i = 0; i < nEnumValues; i++)
            {
                for (int j = i + 1; j < nEnumValues; j++)
                {
                    if (_comparison.DeepEquals(enumValuesElement[i], enumValuesElement[j]))
                    {
                        throw new InvalidSchemaException("Elements in the enum array should be unique.");
                    }
                }
            }
            return new EnumValidator(enumValuesElement);
        }
    }
}
