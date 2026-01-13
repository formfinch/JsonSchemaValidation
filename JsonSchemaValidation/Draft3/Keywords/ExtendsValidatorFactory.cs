// Draft 3 behavior: extends is similar to allOf in later drafts.
// Can be a single schema or an array of schemas.
// Factory for extends keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal class ExtendsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public ExtendsValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "extends";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("extends", out var extendsElement))
            {
                return null;
            }

            List<ISchemaValidator> validators = new();

            if (extendsElement.ValueKind == JsonValueKind.Array)
            {
                // Array of schemas
                if (extendsElement.GetArrayLength() == 0)
                {
                    return null;
                }

                foreach (JsonElement extendsSchemaElement in extendsElement.EnumerateArray())
                {
                    if (extendsSchemaElement.ValueKind != JsonValueKind.Object
                        && extendsSchemaElement.ValueKind != JsonValueKind.False
                        && extendsSchemaElement.ValueKind != JsonValueKind.True)
                    {
                        throw new InvalidSchemaException("Each item of the extends array MUST be a valid JSON Schema.");
                    }

                    var validator = CreateValidator(schemaData, extendsSchemaElement);
                    if (validator == null)
                    {
                        throw new InvalidSchemaException("Each item of the extends array MUST be a valid JSON Schema.");
                    }
                    validators.Add(validator);
                }
            }
            else if (extendsElement.ValueKind == JsonValueKind.Object
                     || extendsElement.ValueKind == JsonValueKind.True
                     || extendsElement.ValueKind == JsonValueKind.False)
            {
                // Single schema
                var validator = CreateValidator(schemaData, extendsElement);
                if (validator == null)
                {
                    throw new InvalidSchemaException("The extends keyword MUST be a valid JSON Schema.");
                }
                validators.Add(validator);
            }
            else
            {
                return null;
            }

            if (!validators.Any())
            {
                return null;
            }

            return new ExtendsValidator(validators, _contextFactory);
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
