using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Validator for the contentEncoding keyword.
    /// This is an annotation-only keyword that produces an annotation
    /// but does not perform validation.
    /// </summary>
    internal class ContentEncodingValidator : IKeywordValidator
    {
        private readonly string _encoding;

        public string Keyword => "contentEncoding";

        public ContentEncodingValidator(string encoding)
        {
            _encoding = encoding;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = new Dictionary<string, object?> { ["contentEncoding"] = _encoding }
            };
        }
    }
}
