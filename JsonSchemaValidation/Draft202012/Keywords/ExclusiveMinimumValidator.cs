using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ExclusiveMinimumValidator : IKeywordValidator
    {
        private const string keyword = "exclusiveMinimum";
        private readonly double minimum;

        public ExclusiveMinimumValidator(double minimum)
        {
            this.minimum = minimum;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Number) return ValidationResult.Ok;
            if (instance.GetDouble() > minimum) return ValidationResult.Ok;
            return new ValidationResult(keyword);
        }
    }
}
