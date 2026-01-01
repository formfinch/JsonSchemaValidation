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
            var ifContext = _contextFactory.CreateFreshContext(context);
            var elseContext = _contextFactory.CreateFreshContext(context);
            var thenContext = _contextFactory.CreateFreshContext(context);

            List<IJsonValidationContext> validContexts = new List<IJsonValidationContext>();

            // Save the scope depth before evaluating any branch
            // Each branch should be isolated - schema resources entered in 'if'
            // should not be visible in 'then' or 'else'
            int scopeDepthBeforeIf = context.Scope.Depth;

            var result = ValidationResult.Ok;
            if (_ifValidator.Validate(ifContext) == ValidationResult.Ok)
            {
                // Restore scope after 'if' - its schema resources should not persist into 'then'
                context.Scope.RestoreToDepth(scopeDepthBeforeIf);

                validContexts.Add(ifContext);
                if (_thenValidator != null)
                {
                    int scopeDepthBeforeThen = context.Scope.Depth;
                    if(_thenValidator.Validate(thenContext) != ValidationResult.Ok)
                    {
                        result = new ValidationResult($"Failed to validate against the 'then' schema in the 'if-then-else' construct.");
                    }
                    else
                    {
                        validContexts.Add(thenContext);
                    }
                    // Restore scope after 'then'
                    context.Scope.RestoreToDepth(scopeDepthBeforeThen);
                }
            }
            else
            {
                // Restore scope after 'if' - its schema resources should not persist into 'else'
                context.Scope.RestoreToDepth(scopeDepthBeforeIf);

                if (_elseValidator != null)
                {
                    int scopeDepthBeforeElse = context.Scope.Depth;
                    if (_elseValidator.Validate(elseContext) != ValidationResult.Ok)
                    {
                        result = new ValidationResult($"Failed to validate against the 'else' schema in the 'if-then-else' construct.");
                    }
                    else
                    {
                        validContexts.Add(elseContext);
                    }
                    // Restore scope after 'else'
                    context.Scope.RestoreToDepth(scopeDepthBeforeElse);
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