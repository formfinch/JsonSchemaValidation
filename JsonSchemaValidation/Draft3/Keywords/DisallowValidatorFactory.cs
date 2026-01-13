// Draft 3 behavior: disallow is the inverse of type.
// Can be a string, array of strings, or array containing schemas.
// Factory for disallow keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal class DisallowValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public DisallowValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "disallow";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("disallow", out var disallowElement))
            {
                return null;
            }

            var typeValidators = new List<IKeywordValidator>();
            var schemaValidators = new List<ISchemaValidator>();

            if (disallowElement.ValueKind == JsonValueKind.String)
            {
                // Single type specification
                string? typeSpec = disallowElement.GetString();
                var validator = TypeValidatorSharedFactory.CreateFromTypeSpecification(typeSpec);
                if (validator != null)
                {
                    typeValidators.Add(validator);
                }
            }
            else if (disallowElement.ValueKind == JsonValueKind.Array)
            {
                // Array of types and/or schemas
                foreach (var item in disallowElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        // Type specification
                        string? typeSpec = item.GetString();
                        var validator = TypeValidatorSharedFactory.CreateFromTypeSpecification(typeSpec);
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
            }
            else
            {
                return null;
            }

            if (typeValidators.Count == 0 && schemaValidators.Count == 0)
            {
                return null;
            }

            return new DisallowValidator(typeValidators, schemaValidators, _contextFactory);
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
