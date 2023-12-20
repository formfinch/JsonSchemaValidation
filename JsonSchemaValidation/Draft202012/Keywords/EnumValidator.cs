using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class EnumValidator : IKeywordValidator
    {
        private const string keyword = "enum";
        private readonly JsonElement _enumValuesElement;
        private static readonly JsonElementComparison _comparison = new();

        public EnumValidator(JsonElement enumValuesElement)
        {
            _enumValuesElement = enumValuesElement;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            foreach(var value in _enumValuesElement.EnumerateArray())
            {
                if (_comparison.DeepEquals(value, context.Data))
                {
                    return ValidationResult.Ok;
                }
            }
            return new ValidationResult(keyword);
        }
    }

}