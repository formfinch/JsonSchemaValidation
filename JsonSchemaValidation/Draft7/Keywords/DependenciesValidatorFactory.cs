// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft 7 behavior: The "dependencies" keyword combines both property dependencies (arrays)
// and schema dependencies (objects/booleans) in a single keyword.
// Factory for dependencies keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
{
    internal class DependenciesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ILazySchemaValidatorFactory _schemaValidatorFactory;

        public DependenciesValidatorFactory(
            ISchemaFactory schemaFactory,
            ILazySchemaValidatorFactory schemaValidatorFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
        }

        public string Keyword => "dependencies";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("dependencies", out var dependenciesElement))
            {
                return null;
            }

            if (dependenciesElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSchemaException("The 'dependencies' keyword must be an object.");
            }

            Dictionary<string, string[]> propertyDependencies = new(StringComparer.Ordinal);
            Dictionary<string, ISchemaValidator> schemaDependencies = new(StringComparer.Ordinal);

            foreach (var dependencyElement in dependenciesElement.EnumerateObject())
            {
                string propertyName = dependencyElement.Name;
                var dependencyValue = dependencyElement.Value;

                if (dependencyValue.ValueKind == JsonValueKind.Array)
                {
                    // Property dependency (array of required property names)
                    var requiredProps = new List<string>();
                    foreach (JsonElement propertyNameElement in dependencyValue.EnumerateArray())
                    {
                        if (propertyNameElement.ValueKind != JsonValueKind.String)
                        {
                            throw new InvalidSchemaException("Property dependencies must be arrays of strings.");
                        }

                        string? reqProp = propertyNameElement.GetString();
                        if (string.IsNullOrWhiteSpace(reqProp))
                        {
                            throw new InvalidSchemaException("Property dependencies cannot have empty property names.");
                        }
                        requiredProps.Add(reqProp!);
                    }

                    if (requiredProps.Count > 0)
                    {
                        propertyDependencies.Add(propertyName, requiredProps.ToArray());
                    }
                    else
                    {
                        // Empty array means no requirements, but we still track it
                        propertyDependencies.Add(propertyName, Array.Empty<string>());
                    }
                }
                else if (dependencyValue.ValueKind == JsonValueKind.Object ||
                         dependencyValue.ValueKind == JsonValueKind.True ||
                         dependencyValue.ValueKind == JsonValueKind.False)
                {
                    // Schema dependency (object or boolean schema)
                    var validator = CreateValidator(schemaData, dependencyValue);
                    schemaDependencies.Add(propertyName, validator);
                }
                else
                {
                    throw new InvalidSchemaException($"Dependency for property '{propertyName}' must be an array or schema.");
                }
            }

            if (propertyDependencies.Count == 0 && schemaDependencies.Count == 0)
            {
                return null;
            }

            return new DependenciesValidator(propertyDependencies, schemaDependencies);
        }

        private ISchemaValidator CreateValidator(SchemaMetadata schemaData, JsonElement schemaElement)
        {
            var subSchemaData = SchemaRepositoryHelpers.CreateSubSchemaMetadata(schemaData, schemaElement);
            var dereferencedSchemaData = _schemaFactory.CreateDereferencedSchema(subSchemaData);

            if (_schemaValidatorFactory.Value == null)
            {
                throw new InvalidOperationException("ISchemaValidatorFactory not initialized");
            }

            return _schemaValidatorFactory.Value.CreateValidator(dereferencedSchemaData);
        }
    }
}
