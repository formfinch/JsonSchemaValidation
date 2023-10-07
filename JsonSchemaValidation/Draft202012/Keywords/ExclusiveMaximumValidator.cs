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
    internal class ExclusiveMaximumValidator : IKeywordValidator
    {
        private const string keyword = "exclusiveMaximum";
        private readonly double maximum;

        public ExclusiveMaximumValidator(double maximum)
        {
            this.maximum = maximum;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if (instance.ValueKind != JsonValueKind.Number) return ValidationResult.Ok;
            if (instance.GetDouble() < maximum) return ValidationResult.Ok;
            return new ValidationResult(keyword);
        }

    }
}
