// Draft behavior: Identical in Draft 7, Draft 2019-09, Draft 2020-12
// Factory for contentEncoding validator.
// When ContentAssertionEnabled is true, this factory creates a combined validator
// that handles both contentEncoding and contentMediaType validation.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
{
    /// <summary>
    /// Factory for the contentEncoding keyword.
    /// When ContentAssertionEnabled is false (default), creates annotation-only validators.
    /// When ContentAssertionEnabled is true, creates validators that perform actual validation.
    /// </summary>
    internal class ContentEncodingValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly SchemaValidationOptions _options;

        public ContentEncodingValidatorFactory(SchemaValidationOptions options)
        {
            _options = options;
        }

        public string Keyword => "contentEncoding";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string? encoding = null;
            string? mediaType = null;

            // Check for contentEncoding
            if (schema.TryGetProperty("contentEncoding", out var contentEncodingElement) &&
                contentEncodingElement.ValueKind == JsonValueKind.String)
            {
                encoding = contentEncodingElement.GetString();
            }

            // Check for contentMediaType (needed for combined assertion validator)
            if (schema.TryGetProperty("contentMediaType", out var contentMediaTypeElement) &&
                contentMediaTypeElement.ValueKind == JsonValueKind.String)
            {
                mediaType = contentMediaTypeElement.GetString();
            }

            // If no contentEncoding, nothing to do
            if (string.IsNullOrEmpty(encoding))
            {
                return null;
            }

            // If content assertion is enabled, create assertion validator
            if (_options.Draft7.ContentAssertionEnabled)
            {
                return new ContentAssertionValidator(encoding, mediaType);
            }

            // Default: annotation-only validator
            return new ContentEncodingValidator(encoding);
        }
    }
}
