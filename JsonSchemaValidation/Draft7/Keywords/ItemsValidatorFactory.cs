// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Different between Draft 4-7/2019-09 and Draft 2020-12
// In Draft 2019-09: "items" can be a schema (all items) OR an array of schemas (tuple validation)
// In Draft 2020-12: "items" is only a schema (applies after prefixItems), tuple validation uses "prefixItems"
// Factory for items keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
{
    internal class ItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public ItemsValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "items";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("items", out var itemsElement))
            {
                return null;
            }

            // In Draft 2019-09, items can be an array (tuple validation) or a schema (all items)
            if (itemsElement.ValueKind == JsonValueKind.Array)
            {
                // Tuple validation mode
                var validators = new List<ISchemaValidator>();
                foreach (JsonElement itemSchema in itemsElement.EnumerateArray())
                {
                    if (itemSchema.ValueKind != JsonValueKind.Object
                        && itemSchema.ValueKind != JsonValueKind.False
                        && itemSchema.ValueKind != JsonValueKind.True)
                    {
                        throw new InvalidSchemaException("Each item in the 'items' array MUST be a valid JSON Schema.");
                    }
                    var validator = CreateValidator(schemaData, itemSchema);
                    validators.Add(validator);
                }

                if (validators.Count == 0)
                {
                    return null;
                }

                return new ItemsArrayValidator(validators, _contextFactory);
            }

            // Single schema mode - applies to all items
            if (itemsElement.ValueKind == JsonValueKind.Object)
            {
                var itemSchemaValidator = CreateValidator(schemaData, itemsElement);
                return new ItemsSchemaValidator(itemSchemaValidator, _contextFactory);
            }

            if (itemsElement.ValueKind == JsonValueKind.False)
            {
                return new ItemsFalseValidator();
            }

            if (itemsElement.ValueKind == JsonValueKind.True)
            {
                return new ItemsTrueValidator();
            }

            throw new InvalidSchemaException("Items has invalid content");
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
