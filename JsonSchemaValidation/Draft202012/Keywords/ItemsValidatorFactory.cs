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
    internal class ItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;

        public ItemsValidatorFactory(ISchemaFactory schemaFactory, ILazySchemaValidatorFactory schemaValidatorFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
        }

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;
            int nPrefixItems = 0;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("items", out var itemsElement))
            {
                return null;
            }

            if (schema.TryGetProperty("prefixItems", out var prefixItemsElement))
            {
                if (prefixItemsElement.ValueKind == JsonValueKind.Array)
                {
                    nPrefixItems = prefixItemsElement.GetArrayLength();
                }
            }

            if (itemsElement.ValueKind == JsonValueKind.Object)
            {
                var itemSchemaValidator = CreateValidator(schemaData, itemsElement);
                return new ItemValidator(itemSchemaValidator, nPrefixItems);
            }

            if (itemsElement.ValueKind == JsonValueKind.Array)
            {
                List<ISchemaValidator> validators = new();
                foreach (JsonElement itemSchemaElement in itemsElement.EnumerateArray())
                {
                    if(itemSchemaElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new InvalidSchemaException("Invalid schema item in items array");

                    }
                    var validator = CreateValidator(schemaData, itemSchemaElement);
                    validators.Add(validator);
                }
                return new ItemsValidator(validators, nPrefixItems);
            }

            if (itemsElement.ValueKind == JsonValueKind.False)
            {
                return new ItemsFalseValidator(nPrefixItems);
            }

            if (itemsElement.ValueKind == JsonValueKind.True)
            {
                return new ItemsTrueValidator();
            }

            throw new InvalidSchemaException("Items has invalid content");
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
