using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Keywords.Logic;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ContainsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public ContainsValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "contains";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("contains", out var containsElement))
            {
                return null;
            }

            if (containsElement.ValueKind != JsonValueKind.Object
                && containsElement.ValueKind != JsonValueKind.False
                && containsElement.ValueKind != JsonValueKind.True)
            {
                throw new InvalidSchemaException($"The keyword value for contains MUST be a valid JSON Schema.");
            }

            var containsSchemaValidator = CreateValidator(schemaData, containsElement);
            if (containsSchemaValidator == null)
            {
                throw new InvalidSchemaException($"The keyword value for contains MUST be a valid JSON Schema.");
            }

            var validator = new ContainsValidator(containsSchemaValidator, _contextFactory);

            if (schema.TryGetNonNegativeInteger("minContains", out var minContains))
            {
                validator.MinContains = minContains;
            }

            if (schema.TryGetNonNegativeInteger("maxContains", out var maxContains))
            {
                validator.MaxContains = maxContains;
            }

            return validator;
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
