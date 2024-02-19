using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("dependentSchemas", out var dependentSchemasElement))
            {
                return null;
            }

            if (dependentSchemasElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSchemaException("The 'depedentSchemas' should consist of an object with schemas.");
            }

            Dictionary<string, ISchemaValidator> dependentSchemasProperties = new();
            foreach(var schemasElement in dependentSchemasElement.EnumerateObject())
            {
                string whenPropertyInObject = schemasElement.Name;

                var validator = CreateValidator(schemaData, schemasElement.Value);
                if (validator == null)
                {
                    throw new InvalidSchemaException("The 'depedentSchemas' should consist of an object with schemas.");
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
