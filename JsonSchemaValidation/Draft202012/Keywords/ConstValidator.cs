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
        private static readonly JsonElementComparison _comparison = new JsonElementComparison();

        public ConstValidator(JsonElement expectedValue)
        {
            _expectedValue = expectedValue;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (_comparison.DeepEquals(_expectedValue, instance))
            {
                return ValidationResult.Ok;
            }
            return new ValidationResult(keyword);
        }
    }

}