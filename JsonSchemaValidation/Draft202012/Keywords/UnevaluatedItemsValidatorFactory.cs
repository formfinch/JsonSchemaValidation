using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class UnevaluatedItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public UnevaluatedItemsValidatorFactory(
            ISchemaFactory schemaFactory, 
            ILazySchemaValidatorFactory schemaValidatorFactory, 
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("unevaluatedItems", out var unevaluatedItemSchemaElement))
            {
                return null;
            }

            if (unevaluatedItemSchemaElement.ValueKind != JsonValueKind.Object
                && unevaluatedItemSchemaElement.ValueKind != JsonValueKind.False
                && unevaluatedItemSchemaElement.ValueKind != JsonValueKind.True)
            {
                throw new InvalidSchemaException("UnevaluatedItems has invalid content");
            }

            var unevaluatedItemValidator = CreateValidator(schemaData, unevaluatedItemSchemaElement);
            if(unevaluatedItemValidator == null)
            {
                throw new InvalidSchemaException("UnevaluatedItems has invalid content");
            }
            return new UnevaluatedItemsValidator(unevaluatedItemValidator, _contextFactory);
        }

        private ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement unevaluatedItemSchemaElement)
        {
            SchemaMetadata prefixItemRawSchemaData = new(schemaData)
            {
                Schema = unevaluatedItemSchemaElement
            };
            var prefixItemDereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(prefixItemRawSchemaData);
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }
            return _schemaValidatorFactory.Value.CreateValidator(prefixItemDereferencedSchemaData);
        }
    }
}
