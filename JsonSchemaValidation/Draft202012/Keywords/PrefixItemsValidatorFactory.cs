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
    internal class PrefixItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;

        public PrefixItemsValidatorFactory(ISchemaFactory schemaFactory, ILazySchemaValidatorFactory schemaValidatorFactory)
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

            if (!schema.TryGetProperty("prefixItems", out var prefixItemsElement))
            {
                return null;
            }

            if (prefixItemsElement.ValueKind == JsonValueKind.Object)
            {
                var itemSchemaValidator = CreateValidator(schemaData, prefixItemsElement);
                return new ItemValidator(itemSchemaValidator, 0);
            }

            if (prefixItemsElement.ValueKind == JsonValueKind.Array)
            {
                List<ISchemaValidator> validators = new();
                
                int idx = 0;
                foreach (JsonElement prefixItemSchemaElement in prefixItemsElement.EnumerateArray())
                {
                    ++idx;
                    if (prefixItemSchemaElement.ValueKind == JsonValueKind.False)
                    {
                        return new PrefixItemFalseValidator(idx - 1);
                    }

                    if (prefixItemSchemaElement.ValueKind == JsonValueKind.True)
                    {
                        // ignore: doesnt do anything
                        continue;
                    }

                    if (prefixItemSchemaElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new InvalidSchemaException("Invalid schema item in prefixItems array");
                    }

                    var validator = CreateValidator(schemaData, prefixItemSchemaElement);
                    validators.Add(validator);
                }
                return new ItemsValidator(validators, 0);
            }

            if (prefixItemsElement.ValueKind == JsonValueKind.False)
            {
                return new PrefixItemFalseValidator(0);
            }

            if (prefixItemsElement.ValueKind == JsonValueKind.True)
            {
                // ignore: doesnt do anything
                return null;
            }

            throw new InvalidSchemaException("PrefixItems has invalid content");
        }

        ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement prefixItemSchemaElement)
        {
            SchemaMetadata prefixItemRawSchemaData = new(schemaData)
            {
                Schema = prefixItemSchemaElement
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
