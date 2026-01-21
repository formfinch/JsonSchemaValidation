// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for additionalProperties keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
{
    internal class AdditionalPropertiesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public AdditionalPropertiesValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "additionalProperties";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("additionalProperties", out var additionalPropertiesElement))
            {
                return null;
            }

            if (additionalPropertiesElement.ValueKind != JsonValueKind.Object
                && additionalPropertiesElement.ValueKind != JsonValueKind.False
                && additionalPropertiesElement.ValueKind != JsonValueKind.True)
            {
                throw new InvalidSchemaException("The value of additionalProperties MUST be a valid JSON Schema.");
            }

            var validator = CreateValidator(schemaData, additionalPropertiesElement);
            if (validator == null)
            {
                throw new InvalidSchemaException("The value of additionalProperties MUST be a valid JSON Schema.");
            }

            IEnumerable<string> propertyNames = Array.Empty<string>();
            schema.TryGetProperty("properties", out var propertiesElement);
            if (propertiesElement.ValueKind == JsonValueKind.Object)
            {
                propertyNames = propertiesElement.EnumerateObject().Select(prp => prp.Name);
            }

            IEnumerable<string> propertyNamePatterns = Array.Empty<string>();
            schema.TryGetProperty("patternProperties", out var patternPropertiesElement);
            if (patternPropertiesElement.ValueKind == JsonValueKind.Object)
            {
                propertyNamePatterns = patternPropertiesElement.EnumerateObject().Select(prp => prp.Name);
            }
            return new AdditionalPropertiesValidator(validator, propertyNames, propertyNamePatterns, _contextFactory);
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
