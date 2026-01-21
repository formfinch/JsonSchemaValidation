// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Factory for creating RefValidator instances.
    /// In Draft 2020-12, $ref is an applicator that works alongside sibling keywords.
    /// </summary>
    internal class RefValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaRepository _schemaRepository;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public RefValidatorFactory(
            ISchemaRepository schemaRepository,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaRepository = schemaRepository;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "$ref";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("$ref", out var refElement))
            {
                return null;
            }

            if (refElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string refValue = refElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(refValue))
            {
                return null;
            }

            return new RefValidator(
                refValue,
                schemaData,
                _schemaRepository,
                _schemaValidatorFactory,
                _contextFactory);
        }
    }
}
