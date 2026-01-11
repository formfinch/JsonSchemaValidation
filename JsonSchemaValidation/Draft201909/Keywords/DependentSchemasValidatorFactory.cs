// Draft behavior: Identical in Draft 2019-09, Draft 2020-12
// Note: In Draft 6 and Draft 7, this functionality was part of the "dependencies" keyword.
// Factory for dependentSchemas keyword validator.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft201909.Keywords
{
    internal class DependentSchemasValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;

        public DependentSchemasValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _ = contextFactory; // Reserved for future use
        }

        public string Keyword => "dependentSchemas";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            bool dependenciesCompatibility = false;
            if (!schema.TryGetProperty("dependentSchemas", out var dependentSchemasElement))
            {
                if (!schema.TryGetProperty("dependencies", out dependentSchemasElement))
                {
                    return null;
                }
                dependenciesCompatibility = true;
            }
            string keyword = dependenciesCompatibility ? "dependencies" : "dependentSchemas";

            if (dependentSchemasElement.ValueKind != JsonValueKind.Object)
            {
                // do not throw for dependencies,
                // dependencies could also be the variant compatible with dependentSchemas
                if (!dependenciesCompatibility)
                {
                    throw new InvalidSchemaException("The 'depedentSchemas' should consist of an object with schemas.");
                }
                return null;
            }

            Dictionary<string, ISchemaValidator> dependentSchemasProperties = new(StringComparer.Ordinal);
            foreach (var schemasElement in dependentSchemasElement.EnumerateObject())
            {
                string whenPropertyInObject = schemasElement.Name;

                var validator = CreateValidator(schemaData, schemasElement.Value);
                if (validator == null)
                {
                    throw new InvalidSchemaException($"The '{keyword}' keyword should consist of an object with schemas.");
                }
                dependentSchemasProperties.Add(whenPropertyInObject, validator);
            }
            return new DependentSchemasValidator(dependentSchemasProperties);
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
