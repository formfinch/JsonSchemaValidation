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
    internal class UnevaluatedPropertiesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public UnevaluatedPropertiesValidatorFactory(
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
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("unevaluatedProperties", out var unevaluatedPropertiesSchemaElement))
            {
                return null;
            }

            if (unevaluatedPropertiesSchemaElement.ValueKind != JsonValueKind.Object
                && unevaluatedPropertiesSchemaElement.ValueKind != JsonValueKind.False
                && unevaluatedPropertiesSchemaElement.ValueKind != JsonValueKind.True)
            {
                throw new InvalidSchemaException("UnevaluatedProperties has invalid content");
            }

            var unevaluatedPropertyValidator = CreateValidator(schemaData, unevaluatedPropertiesSchemaElement);
            if(unevaluatedPropertyValidator == null)
            {
                throw new InvalidSchemaException("UnevaluatedProperties has invalid content");
            }
            return new UnevaluatedPropertiesValidator(unevaluatedPropertyValidator, _contextFactory);
        }

        private ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement unevaluatedPropertySchemaElement)
        {
            SchemaMetadata prefixItemRawSchemaData = new(schemaData)
            {
                Schema = unevaluatedPropertySchemaElement
            };
            var prefixItemDereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(prefixItemRawSchemaData);
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }
            return _schemaValidatorFactory.Value.CreateValidator(prefixItemDereferencedSchemaData);
        }
    }
}
