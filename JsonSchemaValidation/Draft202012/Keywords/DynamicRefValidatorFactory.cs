using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Factory for creating DynamicRefValidator instances.
    /// </summary>
    internal class DynamicRefValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaRepository _schemaRepository;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public DynamicRefValidatorFactory(
            ISchemaRepository schemaRepository,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaRepository = schemaRepository;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "$dynamicRef";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("$dynamicRef", out var dynamicRefElement))
            {
                return null;
            }

            if (dynamicRefElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string dynamicRef = dynamicRefElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dynamicRef))
            {
                return null;
            }

            return new DynamicRefValidator(
                dynamicRef,
                schemaData,
                _schemaRepository,
                _schemaValidatorFactory,
                _contextFactory);
        }
    }
}
