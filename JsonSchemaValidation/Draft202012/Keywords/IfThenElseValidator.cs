using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class IfThenElseValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _ifValidator;
        private readonly ISchemaValidator? _thenValidator;
        private readonly ISchemaValidator? _elseValidator;
        private readonly IJsonValidationContextFactory _contextFactory;
        private readonly bool _requiresTracking;

        public string Keyword => "if";

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
            // Check if any branch requires annotation tracking
            _requiresTracking = _ifValidator.RequiresAnnotationTracking
                || (_thenValidator?.RequiresAnnotationTracking ?? false)
                || (_elseValidator?.RequiresAnnotationTracking ?? false);
        }

        public bool IsValid(IJsonValidationContext context)
        {
            // Use tracking if any branch needs it, or if parent already tracks
            bool needsTracking = _requiresTracking || context is IJsonValidationObjectContext or IJsonValidationArrayContext;

            var ifContext = _contextFactory.CreateFreshContextFast(context, needsTracking);
            int scopeDepthBeforeIf = context.Scope.Depth;

            bool ifValid = _ifValidator.IsValid(ifContext);
            context.Scope.RestoreToDepth(scopeDepthBeforeIf);

            if (ifValid)
            {
                if (_thenValidator != null)
                {
                    var thenContext = _contextFactory.CreateFreshContextFast(context, needsTracking);
                    int scopeDepthBeforeThen = context.Scope.Depth;
                    bool thenValid = _thenValidator.IsValid(thenContext);
                    context.Scope.RestoreToDepth(scopeDepthBeforeThen);
                    if (!thenValid)
                        return false;
                }
            }
            else
            {
                if (_elseValidator != null)
                {
                    var elseContext = _contextFactory.CreateFreshContextFast(context, needsTracking);
                    int scopeDepthBeforeElse = context.Scope.Depth;
                    bool elseValid = _elseValidator.IsValid(elseContext);
                    context.Scope.RestoreToDepth(scopeDepthBeforeElse);
                    if (!elseValid)
                        return false;
                }
            }

            return true;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            var ifContext = _contextFactory.CreateFreshContext(context);
            var validContexts = new List<IJsonValidationContext>();
            var children = new List<ValidationResult>();

            // Save the scope depth before evaluating any branch
            int scopeDepthBeforeIf = context.Scope.Depth;

            // Evaluate 'if' schema
            var ifResult = _ifValidator.Validate(ifContext, keywordLocation);
            children.Add(ifResult);

            if (ifResult.IsValid)
            {
                // Restore scope after 'if'
                context.Scope.RestoreToDepth(scopeDepthBeforeIf);
                validContexts.Add(ifContext);

                if (_thenValidator != null)
                {
                    var thenContext = _contextFactory.CreateFreshContext(context);
                    int scopeDepthBeforeThen = context.Scope.Depth;

                    // Build keyword path for 'then' relative to parent (sibling of 'if')
                    var thenKeywordPath = keywordLocation.Parent().Append("then");
                    var thenResult = _thenValidator.Validate(thenContext, thenKeywordPath);
                    children.Add(thenResult);

                    if (!thenResult.IsValid)
                    {
                        context.Scope.RestoreToDepth(scopeDepthBeforeThen);
                        return ValidationResult.Invalid(instanceLocation, kwLocation, "Failed to validate against the 'then' schema") with { Children = children };
                    }
                    validContexts.Add(thenContext);
                    context.Scope.RestoreToDepth(scopeDepthBeforeThen);
                }
            }
            else
            {
                // Restore scope after 'if'
                context.Scope.RestoreToDepth(scopeDepthBeforeIf);

                if (_elseValidator != null)
                {
                    var elseContext = _contextFactory.CreateFreshContext(context);
                    int scopeDepthBeforeElse = context.Scope.Depth;

                    // Build keyword path for 'else' relative to parent (sibling of 'if')
                    var elseKeywordPath = keywordLocation.Parent().Append("else");
                    var elseResult = _elseValidator.Validate(elseContext, elseKeywordPath);
                    children.Add(elseResult);

                    if (!elseResult.IsValid)
                    {
                        context.Scope.RestoreToDepth(scopeDepthBeforeElse);
                        return ValidationResult.Invalid(instanceLocation, kwLocation, "Failed to validate against the 'else' schema") with { Children = children };
                    }
                    validContexts.Add(elseContext);
                    context.Scope.RestoreToDepth(scopeDepthBeforeElse);
                }
            }

            foreach (var validatedContext in validContexts)
            {
                _contextFactory.CopyAnnotations(validatedContext, context);
            }

            return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children };
        }
    }
}
