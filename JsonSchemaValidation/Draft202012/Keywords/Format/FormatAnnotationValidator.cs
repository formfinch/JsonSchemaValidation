using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
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

        public string Keyword => "format";

        public FormatAnnotationValidator(string format)
        {
            _format = format;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?> { ["format"] = _format }
            };
        }
    }
}
