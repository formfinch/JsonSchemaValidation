using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class DependentRequiredValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("dependentRequired", out var dependentRequiredElement))
            {
                return null;
            }

            Dictionary<string, IEnumerable<string>> dependentRequiredProperties = new();
            foreach(var valueListElement in dependentRequiredElement.EnumerateObject())
            {
                if (valueListElement.Value.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidSchemaException("The 'depedentRequired' should consist of arrays of strings.");
                }

                string whenPropertyInObject = valueListElement.Name;
                List<string> thenRequiredPropertyNames = new List<string>();
                foreach (JsonElement propertyNameElement in valueListElement.Value.EnumerateArray())
                {
                    if (propertyNameElement.ValueKind != JsonValueKind.String)
                    {
                        throw new InvalidSchemaException("The 'depedentRequired' keyword should consist of arrays of strings.");
                    }

                    string? propertyName = propertyNameElement.GetString();
                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        throw new InvalidSchemaException("The 'depedentRequired' keyword does not allow for empty property names.");
                    }
                    thenRequiredPropertyNames.Add(propertyName!);
                }

                if (thenRequiredPropertyNames.Any())
                {
                    dependentRequiredProperties.Add(whenPropertyInObject, thenRequiredPropertyNames);
                }
            }
            return new DependentRequiredValidator(dependentRequiredProperties);
        }
    }
}
