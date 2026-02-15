// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 3, Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for patternProperties keyword validator.

using System.Text.Json;
using System.Text.RegularExpressions;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal class PatternPropertiesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public PatternPropertiesValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "patternProperties";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("patternProperties", out var patternPropertiesElement))
            {
                return null;
            }

            if (patternPropertiesElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSchemaException("The value of patternProperties must be an object contain patterns of property names and their associated property schema.");
            }

            Dictionary<Regex, ISchemaValidator> patternPropertySchemaValidators = new();
            foreach (var propertyElement in patternPropertiesElement.EnumerateObject())
            {
                var validator = CreateValidator(schemaData, propertyElement.Value);
                if (validator == null)
                {
                    throw new InvalidSchemaException("Each property schema of the patternProperties object must be a valid JSON Schema.");
                }
                var regex = EcmaScriptRegexHelper.CreateEcmaScriptRegex(propertyElement.Name);
                patternPropertySchemaValidators.Add(regex, validator);
            }

            if (!patternPropertySchemaValidators.Any())
            {
                return null;
            }

            return new PatternPropertiesValidator(patternPropertySchemaValidators, _contextFactory);
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
