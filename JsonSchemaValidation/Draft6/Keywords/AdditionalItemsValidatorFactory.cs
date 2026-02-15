// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09
// Note: In Draft 2020-12, "additionalItems" was removed and "items" handles this case.
// Factory for additionalItems keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords
{
    internal class AdditionalItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public AdditionalItemsValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "additionalItems";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("additionalItems", out var additionalItemsElement))
            {
                return null;
            }

            // additionalItems only applies when items is an array (tuple validation)
            if (!schema.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            {
                // If items is not an array, additionalItems is ignored
                return null;
            }

            int tupleSize = itemsElement.GetArrayLength();

            if (additionalItemsElement.ValueKind == JsonValueKind.False)
            {
                return new AdditionalItemsFalseValidator(tupleSize);
            }

            if (additionalItemsElement.ValueKind == JsonValueKind.True)
            {
                return new AdditionalItemsTrueValidator(tupleSize);
            }

            if (additionalItemsElement.ValueKind == JsonValueKind.Object)
            {
                var validator = CreateValidator(schemaData, additionalItemsElement);
                return new AdditionalItemsValidator(validator, tupleSize, _contextFactory);
            }

            throw new InvalidSchemaException("The value of 'additionalItems' MUST be a valid JSON Schema.");
        }

        ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement itemSchemaElement)
        {
            var itemsRawSchemaData = SchemaRepositoryHelpers.CreateSubSchemaMetadata(schemaData, itemSchemaElement);

            var itemsDereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(itemsRawSchemaData);
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }
            return _schemaValidatorFactory.Value.CreateValidator(itemsDereferencedSchemaData);
        }
    }
}
