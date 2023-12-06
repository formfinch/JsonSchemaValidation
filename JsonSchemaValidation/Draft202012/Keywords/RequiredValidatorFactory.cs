using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class RequiredValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("required", out var requiredElement))
            {
                return null;
            }

            // Give an error in case of a single string because this is a common mistake
            // and ignoring the required property could lead to unexpected behavior.
            if(requiredElement.ValueKind == JsonValueKind.String)
            {
                throw new InvalidSchemaException("The 'required' keyword should consist of an array of strings.");
            }

            // Standard behavior is obvious misformatting is ignored
            if (requiredElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            List<string> propertyNames = new List<string>();
            foreach (JsonElement propertyNameElement in requiredElement.EnumerateArray())
            {
                if (propertyNameElement.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidSchemaException("The 'required' keyword should consist of an array of strings.");
                }
                
                string? propertyName = propertyNameElement.GetString();
                if(string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new InvalidSchemaException("The 'required' keyword does not allow for empty property names.");
                }
                propertyNames.Add(propertyName!);
            }

            if (!propertyNames.Any())
            {
                return null;
            }

            return new RequiredValidator(propertyNames);
        }
    }
}
