// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class UniqueItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "uniqueItems";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

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

            if (uniqueItemsElement.ValueKind == JsonValueKind.False)
            {
                return null;
            }

            return new UniqueItemsValidator();
        }
    }
}
