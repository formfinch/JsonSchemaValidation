// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for oneOf keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft7.Keywords
{
    internal class OneOfValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public OneOfValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "oneOf";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("oneOf", out var anyOfElement))
            {
                return null;
            }

            if (anyOfElement.ValueKind != JsonValueKind.Array
                || anyOfElement.GetArrayLength() == 0)
            {
                throw new InvalidSchemaException("The keyword value for oneOf MUST be a non-empty array");
            }

            List<ISchemaValidator> validators = new();
            foreach (JsonElement anyOfSchemaElement in anyOfElement.EnumerateArray())
            {
                if (anyOfSchemaElement.ValueKind != JsonValueKind.Object
                    && anyOfSchemaElement.ValueKind != JsonValueKind.False
                    && anyOfSchemaElement.ValueKind != JsonValueKind.True)
                {
                    throw new InvalidSchemaException("Each item of the oneOf array MUST be a valid JSON Schema.");
                }

                var validator = CreateValidator(schemaData, anyOfSchemaElement);
                if (validator == null)
                {
                    throw new InvalidSchemaException("Each item of the anyOf array MUST be a valid JSON Schema.");
                }
                validators.Add(validator);
            }

            if (!validators.Any())
            {
                return null;
            }

            return new OneOfValidator(validators, _contextFactory);

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
