// Draft behavior: Identical in Draft 7, Draft 2019-09, Draft 2020-12
// Note: if/then/else was introduced in Draft 7.
// Factory for if/then/else keyword validators.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Draft201909.Keywords.Logic;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    internal class IfThenElseValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;
        private readonly ISchemaRepository _schemaRepository;

        public IfThenElseValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory,
            ISchemaRepository schemaRepository)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
            _schemaRepository = schemaRepository;
        }

        public string Keyword => "if";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var ifValidator = CreateKeywordValidator(schemaData, "if");
            if (ifValidator == null)
            {
                return null;
            }

            var thenValidator = CreateKeywordValidator(schemaData, "then");
            var elseValidator = CreateKeywordValidator(schemaData, "else");
            return new IfThenElseValidator(ifValidator, thenValidator, elseValidator, _contextFactory);
        }

        private ISchemaValidator? CreateKeywordValidator(SchemaMetadata schemaData, string keyword)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty(keyword, out var keywordElement))
            {
                return null;
            }

            if (keywordElement.ValueKind != JsonValueKind.Object
                && keywordElement.ValueKind != JsonValueKind.False
                && keywordElement.ValueKind != JsonValueKind.True)
            {
                throw new InvalidSchemaException($"The keyword value for {keyword} MUST be a valid JSON Schema.");
            }

            var keywordValidator = CreateValidator(schemaData, keywordElement);
            if (keywordValidator == null)
            {
                throw new InvalidSchemaException($"The keyword value for {keyword} MUST be a valid JSON Schema.");
            }

            return keywordValidator;
        }

        private ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement itemSchemaElement)
        {
            // Check if the original schema element has its own $id BEFORE dereferencing
            // If so, we need to wrap with ScopeAwareSchemaValidator to push that schema resource
            var originalSchemaId = itemSchemaElement.GetIdProperty();
            SchemaMetadata? originalSchemaResource = null;

            // Resolve the $id to get the registered schema resource
            if (!string.IsNullOrEmpty(originalSchemaId)
                && Uri.TryCreate(schemaData.SchemaUri, originalSchemaId, out var schemaResourceUri))
            {
                try
                {
                    originalSchemaResource = _schemaRepository.GetSchema(schemaResourceUri);
                }
                catch
                {
                    // Schema not found, proceed without wrapping
                }
            }

            var itemsRawSchemaData = SchemaRepositoryHelpers.CreateSubSchemaMetadata(schemaData, itemSchemaElement);

            var itemsDereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(itemsRawSchemaData);
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }

            var validator = _schemaValidatorFactory.Value.CreateValidator(itemsDereferencedSchemaData);

            // If the original schema had its own $id, wrap to ensure that schema resource is pushed
            if (originalSchemaResource != null)
            {
                return new ScopeAwareSchemaValidator(validator, originalSchemaResource);
            }

            return validator;
        }
    }
}
