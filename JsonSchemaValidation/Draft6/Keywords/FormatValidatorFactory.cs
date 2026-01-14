// Draft 6 format validator factory.
// In Draft 6, format was annotation-only by default (same as Draft 4-7).
// Format assertion can be enabled via FormatAssertionEnabled option.

using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Draft6.Keywords.Format;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft6.Keywords
{
    internal class FormatValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly SchemaValidationOptions _options;

        /// <summary>
        /// Cached format assertion validators for Draft 6.
        /// Draft 6 defines: date-time, email, hostname, ipv4, ipv6, json-pointer, uri, uri-reference, uri-template.
        /// </summary>
        private static readonly Dictionary<string, IKeywordValidator> CachedAssertionValidators = new(StringComparer.Ordinal)
        {
            { "date-time", new DateTimeValidator() },
            { "email", new EmailValidator() },
            { "hostname", new HostnameValidator(isIDNFormat: false) },
            { "ipv4", new IPAddressValidator(isIPV6Format: false) },
            { "ipv6", new IPAddressValidator(isIPV6Format: true) },
            { "json-pointer", new JsonPointerValidator() },
            { "unknown", new UnknownValidator() },
            { "uri", new UriValidator(iriSupport: false) },
            { "uri-reference", new UriValidator(iriSupport: false, canBeRelative: true) },
            { "uri-template", new UriValidator(isTemplate: true) },
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

            // In Draft 6, format assertion is controlled by the Draft6 FormatAssertionEnabled option.
            // (Draft 6 does not have vocabulary support.)
            if (!_options.Draft6.FormatAssertionEnabled)
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
