using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class DependentSchemasValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;
        private readonly IJsonValidationContextFactory _contextFactory;

        public DependentSchemasValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory,
            IJsonValidationContextFactory contextFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
            _contextFactory = contextFactory;
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

            Dictionary<string, ISchemaValidator> dependentSchemasProperties = new();
            foreach(var schemasElement in dependentSchemasElement.EnumerateObject())
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
            SchemaMetadata itemsRawSchemaData = new(schemaData)
            {
                Schema = itemSchemaElement
            };

            var itemsDereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(itemsRawSchemaData);
            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }
            return _schemaValidatorFactory.Value.CreateValidator(itemsDereferencedSchemaData);
        }

    }
}
