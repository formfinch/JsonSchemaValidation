// Draft 3 behavior: type "any" matches any JSON value.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class TypeAnyValidator : IKeywordValidator
    {
        public static readonly TypeAnyValidator Instance = new();

        public string Keyword => "type";

        public bool SupportsDirectValidation => true;

        private TypeAnyValidator() { }

        public bool IsValid(JsonElement data) => true;

        public bool IsValid(IJsonValidationContext context) => true;

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            return ValidationResult.Valid(context.InstanceLocation.ToString(), keywordLocation.ToString());
        }
    }
}
