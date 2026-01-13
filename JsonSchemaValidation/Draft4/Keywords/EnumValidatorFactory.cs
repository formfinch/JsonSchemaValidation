// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// The enum keyword validates that data equals one of the specified values.

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft4.Keywords
{
    internal class EnumValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "enum";

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

            if (enumValuesElement.ValueKind != JsonValueKind.Array)
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
                    if (JsonElement.DeepEquals(enumValuesElement[i], enumValuesElement[j]))
                    {
                        throw new InvalidSchemaException("Elements in the enum array should be unique.");
                    }
                }
            }
            return new EnumValidator(enumValuesElement);
        }
    }
}
