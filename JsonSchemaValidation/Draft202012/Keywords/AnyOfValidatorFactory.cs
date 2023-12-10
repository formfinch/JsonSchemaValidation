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
    internal class AnyOfValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;

        public AnyOfValidatorFactory(ISchemaFactory schemaFactory, ILazySchemaValidatorFactory schemaValidatorFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
        }

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("anyOf", out var anyOfElement))
            {
                return null;
            }

            if (anyOfElement.ValueKind != JsonValueKind.Array 
                || anyOfElement.GetArrayLength() == 0)
            {
                throw new InvalidSchemaException("The keyword value for anyOf MUST be a non-empty array");
            }

            List<ISchemaValidator> validators = new();
            foreach (JsonElement anyOfSchemaElement in anyOfElement.EnumerateArray())
            {
                if (anyOfSchemaElement.ValueKind != JsonValueKind.Object
                    && anyOfSchemaElement.ValueKind != JsonValueKind.False
                    && anyOfSchemaElement.ValueKind != JsonValueKind.True)
                {
                    throw new InvalidSchemaException("Each item of the anyOf array MUST be a valid JSON Schema.");
                }

                var validator = CreateValidator(schemaData, anyOfSchemaElement);
                if(validator == null)
                {
                    throw new InvalidSchemaException("Each item of the anyOf array MUST be a valid JSON Schema.");
                }
                validators.Add(validator);
            }

            if(!validators.Any())
            {
                return null;
            }

            return new AnyOfValidator(validators);

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
