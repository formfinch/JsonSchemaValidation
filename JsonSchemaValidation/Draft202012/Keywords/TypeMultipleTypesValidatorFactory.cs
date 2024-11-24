using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeMultipleTypesValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
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
                var validator  = TypeValidatorSharedFactory.CreateFromTypeSpecification(typeSpecification);
                if (validator != null)
                {
                    validators.Add(validator);
                }
            }
            return new TypeMultipleTypesValidator(validators);
        }
    }
}
