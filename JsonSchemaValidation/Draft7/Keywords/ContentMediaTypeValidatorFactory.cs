// Draft behavior: Identical in Draft 7, Draft 2019-09, Draft 2020-12
// Factory for contentMediaType validator.
// When ContentAssertionEnabled is true and contentEncoding is NOT present,
// this factory creates the assertion validator for contentMediaType only.

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft7.Keywords
{
    /// <summary>
    /// Factory for the contentMediaType keyword.
    /// When ContentAssertionEnabled is false (default), creates annotation-only validators.
    /// When ContentAssertionEnabled is true, creates assertion validators
    /// (but only if contentEncoding is not present, since ContentEncodingValidatorFactory handles the combined case).
    /// </summary>
    internal class ContentMediaTypeValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly SchemaValidationOptions _options;

        public ContentMediaTypeValidatorFactory(SchemaValidationOptions options)
        {
            _options = options;
        }

        public string Keyword => "contentMediaType";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("contentMediaType", out var contentMediaTypeElement))
            {
                return null;
            }

            // contentMediaType must be a string per the spec
            if (contentMediaTypeElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var mediaType = contentMediaTypeElement.GetString();
            if (string.IsNullOrEmpty(mediaType))
            {
                return null;
            }

            // If content assertion is enabled
            if (_options.ContentAssertionEnabled)
            {
                // Check if contentEncoding is also present
                bool hasEncoding = schema.TryGetProperty("contentEncoding", out var encodingElement) &&
                                   encodingElement.ValueKind == JsonValueKind.String &&
                                   !string.IsNullOrEmpty(encodingElement.GetString());

                if (hasEncoding)
                {
                    // ContentEncodingValidatorFactory handles the combined case
                    return null;
                }

                // No encoding - create assertion validator for mediaType only
                return new ContentAssertionValidator(null, mediaType);
            }

            // Default: annotation-only validator
            return new ContentMediaTypeValidator(mediaType);
        }
    }
}
