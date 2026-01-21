// Draft 7 behavior: Content keywords can optionally perform validation.
// This validator performs actual validation (not just annotation).
// It validates contentEncoding (base64) and contentMediaType (application/json).

using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
{
    /// <summary>
    /// Validator that performs actual validation for content keywords.
    /// Handles contentEncoding (base64) and contentMediaType (application/json).
    /// </summary>
    internal sealed class ContentAssertionValidator : IKeywordValidator
    {
        private readonly string? _encoding;
        private readonly string? _mediaType;
        private readonly bool _isBase64Encoding;
        private readonly bool _isJsonMediaType;

        public string Keyword => "content";

        public ContentAssertionValidator(string? encoding, string? mediaType)
        {
            _encoding = encoding;
            _mediaType = mediaType;
            _isBase64Encoding = string.Equals(encoding, "base64", StringComparison.OrdinalIgnoreCase);
            _isJsonMediaType = string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsValid(IJsonValidationContext context) => IsValid(context.Data);

        public bool IsValid(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.String)
                return true;

            var value = data.GetString();
            if (value == null)
                return true;

            return ValidateContent(value);
        }

        private bool ValidateContent(string value)
        {
            string contentToValidate = value;

            // If base64 encoding is specified, validate and decode
            if (_isBase64Encoding)
            {
                if (!TryDecodeBase64(value, out var decoded))
                    return false;
                contentToValidate = decoded ?? string.Empty;
            }

            // If JSON media type is specified, validate the content
            if (_isJsonMediaType && !IsValidJson(contentToValidate))
            {
                return false;
            }

            return true;
        }

        private static bool TryDecodeBase64(string value, out string? decoded)
        {
            decoded = null;
            try
            {
                // Check if string contains only valid base64 characters
                // Base64 alphabet: A-Z, a-z, 0-9, +, /, and = for padding
#pragma warning disable S3267 // Loop has early return for performance
                foreach (char c in value)
                {
                    if (!char.IsLetterOrDigit(c) && c != '+' && c != '/' && c != '=' && !char.IsWhiteSpace(c))
                        return false;
                }
#pragma warning restore S3267

                var bytes = Convert.FromBase64String(value);
                decoded = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool IsValidJson(string value)
        {
            try
            {
                using var doc = JsonDocument.Parse(value);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.String)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            var value = context.Data.GetString();
            if (value == null)
            {
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            string contentToValidate = value;

            // If base64 encoding is specified, validate and decode
            if (_isBase64Encoding)
            {
                if (!TryDecodeBase64(value, out var decoded))
                {
                    return ValidationResult.Invalid(instanceLocation, kwLocation,
                        $"Value is not valid {_encoding} encoding");
                }
                contentToValidate = decoded ?? string.Empty;
            }

            // If JSON media type is specified, validate the content
            if (_isJsonMediaType && !IsValidJson(contentToValidate))
            {
                return ValidationResult.Invalid(instanceLocation, kwLocation,
                    $"Value is not valid {_mediaType}");
            }

            // Build annotations
            var annotations = new Dictionary<string, object?>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(_encoding))
                annotations["contentEncoding"] = _encoding;
            if (!string.IsNullOrEmpty(_mediaType))
                annotations["contentMediaType"] = _mediaType;

            return ValidationResult.Valid(instanceLocation, kwLocation) with
            {
                Annotations = annotations.Count > 0 ? annotations : null
            };
        }
    }
}
