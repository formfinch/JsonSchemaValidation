// Draft 3 behavior: Properties validates object properties AND handles required boolean.
// In Draft 3, "required" is a boolean on each property definition, not an array at schema level.
// Factory for properties keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft3.Keywords
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
            HashSet<string> requiredProperties = new(StringComparer.Ordinal);

            foreach (var propertyElement in propertiesElement.EnumerateObject())
            {
                var validator = CreateValidator(schemaData, propertyElement.Value);
                if (validator == null)
                {
                    throw new InvalidSchemaException("Each property schema of the properties object must be a valid JSON Schema.");
                }
                propertySchemaValidators.Add(propertyElement.Name, validator);

                // Check if this property has "required": true (Draft 3 behavior)
                if (propertyElement.Value.ValueKind == JsonValueKind.Object
                    && propertyElement.Value.TryGetProperty("required", out var requiredElement)
                    && requiredElement.ValueKind == JsonValueKind.True)
                {
                    requiredProperties.Add(propertyElement.Name);
                }
            }

            if (!propertySchemaValidators.Any())
            {
                return null;
            }

            return new PropertiesValidator(propertySchemaValidators, requiredProperties, _contextFactory);
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
