// Draft 3 behavior: Handles array type specification.
// In Draft 3, the type array can contain both type strings AND schemas.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal class TypeMultipleTypesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public TypeMultipleTypesValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "type";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("type", out var typeElement))
            {
                return null;
            }

            if (typeElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var typeValidators = new List<IKeywordValidator>();
            var schemaValidators = new List<ISchemaValidator>();

            foreach (var item in typeElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    // Type specification
                    string? typeSpecification = item.GetString();
                    var validator = TypeValidatorSharedFactory.CreateFromTypeSpecification(typeSpecification);
                    if (validator != null)
                    {
                        typeValidators.Add(validator);
                    }
                }
                else if (item.ValueKind == JsonValueKind.Object
                         || item.ValueKind == JsonValueKind.True
                         || item.ValueKind == JsonValueKind.False)
                {
                    // Schema
                    var validator = CreateSchemaValidator(schemaData, item);
                    if (validator != null)
                    {
                        schemaValidators.Add(validator);
                    }
                }
            }

            if (typeValidators.Count == 0 && schemaValidators.Count == 0)
            {
                return null;
            }

            return new TypeMultipleTypesValidator(typeValidators, schemaValidators, _contextFactory);
        }

        private ISchemaValidator CreateSchemaValidator(SchemaMetadata schemaData, JsonElement schemaElement)
        {
            var subSchemaData = SchemaRepositoryHelpers.CreateSubSchemaMetadata(schemaData, schemaElement);
            var dereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(subSchemaData);

            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }

            return _schemaValidatorFactory.Value.CreateValidator(dereferencedSchemaData);
        }
    }
}
