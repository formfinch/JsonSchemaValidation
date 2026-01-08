using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
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
