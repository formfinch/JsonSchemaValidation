// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public ItemsValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "items";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;
            int nPrefixItems = 0;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("items", out var itemsElement))
            {
                return null;
            }

            if (schema.TryGetProperty("prefixItems", out var prefixItemsElement)
                && prefixItemsElement.ValueKind == JsonValueKind.Array)
            {
                nPrefixItems = prefixItemsElement.GetArrayLength();
            }

            if (itemsElement.ValueKind == JsonValueKind.Object)
            {
                var itemSchemaValidator = CreateValidator(schemaData, itemsElement);
                return new ItemValidator(itemSchemaValidator, nPrefixItems, _contextFactory);
            }

            if (itemsElement.ValueKind == JsonValueKind.False)
            {
                return new ItemsFalseValidator(nPrefixItems);
            }

            if (itemsElement.ValueKind == JsonValueKind.True)
            {
                return new ItemsTrueValidator(nPrefixItems);
            }

            throw new InvalidSchemaException("Items has invalid content");
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
