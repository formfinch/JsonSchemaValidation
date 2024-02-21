using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class IfThenElseValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public IfThenElseValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var ifValidator = CreateKeywordValidator(schemaData, "if");
            if(ifValidator == null)
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
