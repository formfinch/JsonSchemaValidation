using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ConstValidator : IKeywordValidator
    {
        private const string keyword = "const";
        private readonly JsonElement _expectedValue;
        private static readonly JsonElementComparison _comparison = new();

        public ConstValidator(JsonElement expectedValue)
        {
            _expectedValue = expectedValue;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (_comparison.DeepEquals(_expectedValue, context.Data))
            {
                return ValidationResult.Ok;
            }
            return new ValidationResult(keyword);
        }
    }

}