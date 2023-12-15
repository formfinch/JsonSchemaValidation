using JsonSchemaValidation.Abstractions;
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
    internal class MaximumValidator : IKeywordValidator
    {
        private const string keyword = "maximum";
        private readonly double maximum;

        public MaximumValidator(double maximum)
        {
            this.maximum = maximum;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if (context.Data.ValueKind != JsonValueKind.Number) return ValidationResult.Ok;
            if (context.Data.GetDouble() <= maximum) return ValidationResult.Ok;
            return new ValidationResult(keyword);
        }
    }
}
