// Draft behavior: Identical in Draft 2019-09, Draft 2020-12
// Note: unevaluatedProperties was introduced in Draft 2019-09.
// Factory for unevaluatedProperties keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft201909.Keywords
{
    internal class UnevaluatedPropertiesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public UnevaluatedPropertiesValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "unevaluatedProperties";

        public int ExecutionOrder => 100;

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("unevaluatedProperties", out var unevaluatedPropertiesSchemaElement))
            {
                return null;
            }

            if (unevaluatedPropertiesSchemaElement.ValueKind != JsonValueKind.Object
                && unevaluatedPropertiesSchemaElement.ValueKind != JsonValueKind.False
                && unevaluatedPropertiesSchemaElement.ValueKind != JsonValueKind.True)
            {
                throw new InvalidSchemaException("UnevaluatedProperties has invalid content");
            }

            var unevaluatedPropertyValidator = CreateValidator(schemaData, unevaluatedPropertiesSchemaElement);
            if (unevaluatedPropertyValidator == null)
            {
                throw new InvalidSchemaException("UnevaluatedProperties has invalid content");
            }
            return new UnevaluatedPropertiesValidator(unevaluatedPropertyValidator, _contextFactory);
        }

        private ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement unevaluatedPropertySchemaElement)
        {
            var prefixItemRawSchemaData = SchemaRepositoryHelpers.CreateSubSchemaMetadata(schemaData, unevaluatedPropertySchemaElement);
            var prefixItemDereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(prefixItemRawSchemaData);
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }
            return _schemaValidatorFactory.Value.CreateValidator(prefixItemDereferencedSchemaData);
        }
    }
}
