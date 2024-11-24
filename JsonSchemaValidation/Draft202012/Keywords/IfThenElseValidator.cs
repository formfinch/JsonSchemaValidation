using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class IfThenElseValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _ifValidator;
        private readonly ISchemaValidator? _thenValidator;
        private readonly ISchemaValidator? _elseValidator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public IfThenElseValidator(
            ISchemaValidator ifValidator, 
            ISchemaValidator? thenValidator, 
            ISchemaValidator? elseValidator,
            IJsonValidationContextFactory contextFactory)
        {
            _ifValidator = ifValidator;
            _thenValidator = thenValidator;
            _elseValidator = elseValidator;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var ifContext = _contextFactory.CopyContext(context);
            var elseContext = _contextFactory.CopyContext(context);
            var thenContext = _contextFactory.CopyContext(context);

            List<IJsonValidationContext> validContexts = new List<IJsonValidationContext>();

            var result = ValidationResult.Ok;
            if (_ifValidator.Validate(ifContext) == ValidationResult.Ok)
            {
                validContexts.Add(ifContext);
                if (_thenValidator != null)
                {
                    if(_thenValidator.Validate(thenContext) != ValidationResult.Ok)
                    {
                        result = new ValidationResult($"Failed to validate against the 'then' schema in the 'if-then-else' construct.");
                    }
                    else
                    {
                        validContexts.Add(thenContext);
                    }
                }
            }
            else
            {
                if (_elseValidator != null)
                {
                    if (_elseValidator.Validate(elseContext) != ValidationResult.Ok)
                    {
                        result = new ValidationResult($"Failed to validate against the 'else' schema in the 'if-then-else' construct.");
                    }
                    else
                    {
                        validContexts.Add(elseContext);
                    }
                }
            }

            foreach(var validatedContext in validContexts)
            {
                _contextFactory.CopyAnnotations(validatedContext, context);
            }
            return result;
        }
    }
}