// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal class PrefixItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public PrefixItemsValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "prefixItems";

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

            if (prefixItemsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidSchemaException("PrefixItems has invalid content");
            }

            List<ISchemaValidator> validators = new();
            foreach (JsonElement prefixItemSchemaElement in prefixItemsElement.EnumerateArray())
            {
                if (prefixItemSchemaElement.ValueKind != JsonValueKind.Object
                    && prefixItemSchemaElement.ValueKind != JsonValueKind.False
                    && prefixItemSchemaElement.ValueKind != JsonValueKind.True)
                {
                    throw new InvalidSchemaException("Invalid schema item in prefixItems array");
                }

                var validator = CreateValidator(schemaData, prefixItemSchemaElement);
                validators.Add(validator);

            }
            return new PrefixItemsValidator(validators, _contextFactory);
        }

        private ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement prefixItemSchemaElement)
        {
            var prefixItemRawSchemaData = SchemaRepositoryHelpers.CreateSubSchemaMetadata(schemaData, prefixItemSchemaElement);
            var prefixItemDereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(prefixItemRawSchemaData);
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }
            return _schemaValidatorFactory.Value.CreateValidator(prefixItemDereferencedSchemaData);
        }
    }
}
