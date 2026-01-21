// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
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
