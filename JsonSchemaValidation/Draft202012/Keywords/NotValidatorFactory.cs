using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class NotValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public NotValidatorFactory(ISchemaFactory schemaFactory, 
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

            if (!schema.TryGetProperty("not", out var notElement))
            {
                return null;
            }

            if (notElement.ValueKind != JsonValueKind.Object
                && notElement.ValueKind != JsonValueKind.False
                && notElement.ValueKind != JsonValueKind.True)
            {
                throw new InvalidSchemaException("The keyword value for not MUST be a valid JSON Schema.");
            }

            var validator = CreateValidator(schemaData, notElement);
            if(validator == null)
            {
                throw new InvalidSchemaException("The keyword value for not MUST be a valid JSON Schema.");
            }
            return new NotValidator(validator, _contextFactory);

        }

        ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement itemSchemaElement)
        {
            SchemaMetadata itemsRawSchemaData = new(schemaData)
            {
                Schema = itemSchemaElement
            };

            var itemsDereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(itemsRawSchemaData);
            if(_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }
            return _schemaValidatorFactory.Value.CreateValidator(itemsDereferencedSchemaData);
        }
    }
}
