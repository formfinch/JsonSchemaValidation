using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PropertiesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public PropertiesValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "properties";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("properties", out var propertiesElement))
            {
                return null;
            }

            if (propertiesElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSchemaException("Properties keyword must be an object containing property names and their associated property schema.");
            }


            Dictionary<string, ISchemaValidator> propertySchemaValidators = new(StringComparer.Ordinal);
            foreach (var propertyElement in propertiesElement.EnumerateObject())
            {
                var validator = CreateValidator(schemaData, propertyElement.Value);
                if (validator == null)
                {
                    throw new InvalidSchemaException("Each property schema of the properties object must be a valid JSON Schema.");
                }
                propertySchemaValidators.Add(propertyElement.Name, validator);
            }

            if (!propertySchemaValidators.Any())
            {
                return null;
            }

            return new PropertiesValidator(propertySchemaValidators, _contextFactory);
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
