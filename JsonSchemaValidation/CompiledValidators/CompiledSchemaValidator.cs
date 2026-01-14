using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.CompiledValidators;

/// <summary>
/// Adapter that wraps an ICompiledValidator to implement ISchemaValidator.
/// Uses the compiled validator for fast IsValid checks and lazily creates
/// a dynamic validator for detailed Validate results when needed.
/// </summary>
internal sealed class CompiledSchemaValidator : ISchemaValidator
{
    private readonly ICompiledValidator _compiledValidator;
    private readonly Lazy<ISchemaValidator> _fallbackValidator;

    public CompiledSchemaValidator(
        ICompiledValidator compiledValidator,
        Func<ISchemaValidator> fallbackValidatorFactory)
    {
        _compiledValidator = compiledValidator;
        _fallbackValidator = new Lazy<ISchemaValidator>(fallbackValidatorFactory);
    }

    /// <inheritdoc />
    public bool RequiresAnnotationTracking => _fallbackValidator.Value.RequiresAnnotationTracking;

    /// <inheritdoc />
    public bool IsValid(IJsonValidationContext context)
    {
        // Use the compiled validator for fast path
        return _compiledValidator.IsValid(context.Data);
    }

    /// <inheritdoc />
    public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
    {
        // Fall back to dynamic validator for detailed results
        return _fallbackValidator.Value.Validate(context, keywordLocation);
    }

    /// <inheritdoc />
    public void AddKeywordValidator(IKeywordValidator keywordValidator)
    {
        throw new NotSupportedException("Compiled validators do not support adding keyword validators.");
    }
}
