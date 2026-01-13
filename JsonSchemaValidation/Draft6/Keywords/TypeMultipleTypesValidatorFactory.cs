// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Handles array type specification (e.g., "type": ["string", "null"]).

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft6.Keywords
{
    internal class TypeMultipleTypesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "type";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("type", out var typeElement))
            {
                return null;
            }

            if (typeElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var validators = new List<IKeywordValidator>();
            var typeKeywords = typeElement.EnumerateArray();
            while (typeKeywords.MoveNext())
            {
                if (typeKeywords.Current.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidSchemaException("Type array should contain only type specifications");
                }

                string? typeSpecification = typeKeywords.Current.GetString();
                var validator = TypeValidatorSharedFactory.CreateFromTypeSpecification(typeSpecification);
                if (validator != null)
                {
                    validators.Add(validator);
                }
            }
            return new TypeMultipleTypesValidator(validators);
        }
    }
}
