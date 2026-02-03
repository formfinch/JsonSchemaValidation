// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Identical in Draft 2019-09, Draft 2020-12
// Note: In Draft 6 and Draft 7, this functionality was part of the "dependencies" keyword.
// Factory for dependentRequired keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    internal class DependentRequiredValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "dependentRequired";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            bool dependenciesCompatibility = false;
            if (!schema.TryGetProperty("dependentRequired", out var dependentRequiredElement))
            {
                if (!schema.TryGetProperty("dependencies", out dependentRequiredElement))
                {
                    return null;
                }

                dependenciesCompatibility = true;
            }
            string keyword = dependenciesCompatibility ? "dependencies" : "dependentRequired";

            Dictionary<string, List<string>> dependentRequiredProperties = new(StringComparer.Ordinal);
            foreach (var valueListElement in dependentRequiredElement.EnumerateObject())
            {
                if (valueListElement.Value.ValueKind != JsonValueKind.Array)
                {
                    // do not throw for dependencies,
                    // dependencies could also be the variant compatible with dependentSchemas
                    if (!dependenciesCompatibility)
                    {
                        throw new InvalidSchemaException("The 'depedentRequired' should consist of arrays of strings.");
                    }
                    return null;
                }

                string whenPropertyInObject = valueListElement.Name;
                List<string> thenRequiredPropertyNames = new List<string>();
                foreach (JsonElement propertyNameElement in valueListElement.Value.EnumerateArray())
                {
                    if (propertyNameElement.ValueKind != JsonValueKind.String)
                    {
                        throw new InvalidSchemaException($"The '{keyword}' keyword should consist of arrays of strings.");
                    }

                    string? propertyName = propertyNameElement.GetString();
                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        throw new InvalidSchemaException($"The '{keyword}' keyword does not allow for empty property names.");
                    }
                    thenRequiredPropertyNames.Add(propertyName!);
                }

                if (thenRequiredPropertyNames.Count > 0)
                {
                    dependentRequiredProperties.Add(whenPropertyInObject, thenRequiredPropertyNames);
                }
            }
            return new DependentRequiredValidator(dependentRequiredProperties);
        }
    }
}
