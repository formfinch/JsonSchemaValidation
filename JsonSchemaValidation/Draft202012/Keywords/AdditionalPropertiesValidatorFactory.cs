// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
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

            HashSet<string> propertyNames = new(StringComparer.Ordinal);
            schema.TryGetProperty("properties", out var propertiesElement);
            if (propertiesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prp in propertiesElement.EnumerateObject())
                {
                    propertyNames.Add(prp.Name);
                }
            }

            List<Regex> patternRegexes = [];
            schema.TryGetProperty("patternProperties", out var patternPropertiesElement);
            if (patternPropertiesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prp in patternPropertiesElement.EnumerateObject())
                {
                    patternRegexes.Add(EcmaScriptRegexHelper.CreateEcmaScriptRegex(prp.Name));
                }
            }
            return new AdditionalPropertiesValidator(validator, propertyNames, patternRegexes, _contextFactory);
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
