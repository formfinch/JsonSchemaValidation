using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    /// <summary>
    /// Annotation-only format validator.
    /// Per Draft 2020-12 spec, the format keyword is annotation-only by default.
    /// This validator only produces an annotation and always returns valid.
    /// </summary>
    internal class FormatAnnotationValidator : IKeywordValidator
    {
        private readonly string _format;

        public FormatAnnotationValidator(string format)
        {
            _format = format;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            var result = new ValidationResult();
            result.SetAnnotation("format", _format);
            return result;
        }
    }
}
