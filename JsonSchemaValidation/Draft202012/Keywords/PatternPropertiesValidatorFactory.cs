using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PatternPropertiesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public PatternPropertiesValidatorFactory(
            ISchemaFactory schemaFactory, 
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "patternProperties";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("patternProperties", out var patternPropertiesElement))
            {
                return null;
            }

            if(patternPropertiesElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSchemaException("The value of patternProperties must be an object contain patterns of property names and their associated property schema.");
            }

            Dictionary<string, ISchemaValidator> patternPropertySchemaValidators = new();
            foreach (var propertyElement in patternPropertiesElement.EnumerateObject())
            {
                var validator = CreateValidator(schemaData, propertyElement.Value);
                if(validator == null)
                {
                    throw new InvalidSchemaException("Each property schema of the patternProperties object must be a valid JSON Schema.");
                }
                patternPropertySchemaValidators.Add(propertyElement.Name, validator);
            }

            if(!patternPropertySchemaValidators.Any())
            {
                return null;
            }

            return new PatternPropertiesValidator(patternPropertySchemaValidators, _contextFactory);
        }

        ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement itemSchemaElement)
        {
            var itemsRawSchemaData = SchemaRepositoryHelpers.CreateSubSchemaMetadata(schemaData, itemSchemaElement);

            var itemsDereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(itemsRawSchemaData);
            if(_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }
            return _schemaValidatorFactory.Value.CreateValidator(itemsDereferencedSchemaData);
        }
    }
}
