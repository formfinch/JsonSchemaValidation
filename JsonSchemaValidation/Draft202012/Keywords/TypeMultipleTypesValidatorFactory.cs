using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
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
