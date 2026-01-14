// Draft 4 format validator factory.
// In Draft 4, format was annotation-only by default.
// Format assertion can be enabled via FormatAssertionEnabled option.
// Draft 4 only defines: date-time, email, hostname, ipv4, ipv6, uri.

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Draft4.Keywords.Format;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft4.Keywords
{
    internal class FormatValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly SchemaValidationOptions _options;

        /// <summary>
        /// Cached format assertion validators for Draft 4.
        /// Draft 4 defines: date-time, email, hostname, ipv4, ipv6, uri.
        /// Note: json-pointer, uri-reference, uri-template were added in Draft 6.
        /// </summary>
        private static readonly Dictionary<string, IKeywordValidator> CachedAssertionValidators = new(StringComparer.Ordinal)
        {
            { "date-time", new DateTimeValidator() },
            { "email", new EmailValidator() },
            { "hostname", new HostnameValidator(isIDNFormat: false) },
            { "ipv4", new IPAddressValidator(isIPV6Format: false) },
            { "ipv6", new IPAddressValidator(isIPV6Format: true) },
            { "unknown", new UnknownValidator() },
            { "uri", new UriValidator(iriSupport: false) },
        };

        public FormatValidatorFactory(SchemaValidationOptions options)
        {
            _options = options;
        }

        public string Keyword => "format";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("format", out var formatElement))
            {
                return null;
            }

            if (formatElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? format = formatElement.GetString();
            if (string.IsNullOrEmpty(format))
            {
                throw new InvalidSchemaException("The format annotation attribute must be a string.");
            }

            // In Draft 4, format assertion is controlled by the Draft4 FormatAssertionEnabled option.
            // (Draft 4 does not have vocabulary support.)
            if (!_options.Draft4.FormatAssertionEnabled)
            {
                return new FormatAnnotationValidator(format);
            }

            // Use cached validator instances for known formats (they are stateless)
            if (CachedAssertionValidators.TryGetValue(format, out var cachedValidator))
            {
                return cachedValidator;
            }

            // Unknown format - return annotation-only validator
            return new FormatAnnotationValidator(format);
        }
    }
}
