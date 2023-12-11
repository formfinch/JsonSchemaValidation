using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class IfThenElseValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _ifValidator;
        private readonly ISchemaValidator? _thenValidator;
        private readonly ISchemaValidator? _elseValidator;

        public IfThenElseValidator(
            ISchemaValidator ifValidator, 
            ISchemaValidator? thenValidator, 
            ISchemaValidator? elseValidator)
        {
            _ifValidator = ifValidator;
            _thenValidator = thenValidator;
            _elseValidator = elseValidator;
        }

        public ValidationResult Validate(JsonElement instance)
        {
            if(_ifValidator.Validate(instance) == ValidationResult.Ok)
            {
                if(_thenValidator != null && _thenValidator.Validate(instance) != ValidationResult.Ok)
                {
                    return new ValidationResult($"Failed to validate against the 'then' schema in the 'if-then-else' construct.");
                }
            }
            else
            {
                if (_elseValidator != null && _elseValidator.Validate(instance) != ValidationResult.Ok)
                {
                    return new ValidationResult($"Failed to validate against the 'else' schema in the 'if-then-else' construct.");
                }
            }
            return ValidationResult.Ok;
        }
    }
}