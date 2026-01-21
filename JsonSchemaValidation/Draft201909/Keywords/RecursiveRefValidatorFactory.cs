// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Specific to Draft 2019-09
// Note: In Draft 2020-12, $recursiveRef was replaced by $dynamicRef with different semantics.
// Factory for creating RecursiveRefValidator instances.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    internal class RecursiveRefValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaRepository _schemaRepository;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public RecursiveRefValidatorFactory(
            ISchemaRepository schemaRepository,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaRepository = schemaRepository;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
        }

        public string Keyword => "$recursiveRef";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("$recursiveRef", out var recursiveRefElement))
            {
                return null;
            }

            if (recursiveRefElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string recursiveRef = recursiveRefElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(recursiveRef))
            {
                return null;
            }

            return new RecursiveRefValidator(
                recursiveRef,
                schemaData,
                _schemaRepository,
                _schemaValidatorFactory,
                _contextFactory);
        }
    }
}
