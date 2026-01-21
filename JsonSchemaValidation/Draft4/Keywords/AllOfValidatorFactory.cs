// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for allOf keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft4.Keywords
{
    internal class AllOfValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public AllOfValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "allOf";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("allOf", out var allOfElement))
            {
                return null;
            }

            if (allOfElement.ValueKind != JsonValueKind.Array
                || allOfElement.GetArrayLength() == 0)
            {
                throw new InvalidSchemaException("The keyword value for allOf MUST be a non-empty array");
            }

            List<ISchemaValidator> validators = new();
            foreach (JsonElement allOfSchemaElement in allOfElement.EnumerateArray())
            {
                if (allOfSchemaElement.ValueKind != JsonValueKind.Object
                    && allOfSchemaElement.ValueKind != JsonValueKind.False
                    && allOfSchemaElement.ValueKind != JsonValueKind.True)
                {
                    throw new InvalidSchemaException("Each item of the allOf array MUST be a valid JSON Schema.");
                }

                var validator = CreateValidator(schemaData, allOfSchemaElement);
                if (validator == null)
                {
                    throw new InvalidSchemaException("Each item of the allOf array MUST be a valid JSON Schema.");
                }
                validators.Add(validator);
            }

            if (!validators.Any())
            {
                return null;
            }

            return new AllOfValidator(validators, _contextFactory);

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
