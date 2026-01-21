// Draft 3 format validator factory.
// In Draft 3, format was annotation-only by default.
// Format assertion can be enabled via FormatAssertionEnabled option.
// Draft 3 defines: date-time, email, ip-address, ipv6, host-name, uri, regex, date, time, color.
// Note: Draft 3 uses "ip-address" (not "ipv4") and "host-name" (not "hostname").

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Draft3.Keywords.Format;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft3.Keywords
{
    internal class FormatValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly SchemaValidationOptions _options;

        /// <summary>
        /// Cached format assertion validators for Draft 3.
        /// Draft 3 defines: date-time, email, ip-address, ipv6, host-name, uri, regex, date, time, color.
        /// Note: Draft 3 uses "ip-address" instead of "ipv4" and "host-name" instead of "hostname".
        /// </summary>
        private static readonly Dictionary<string, IKeywordValidator> CachedAssertionValidators = new(StringComparer.Ordinal)
        {
            { "date-time", new DateTimeValidator() },
            { "email", new EmailValidator() },
            { "ip-address", new IPAddressValidator(isIPV6Format: false) },
            { "ipv6", new IPAddressValidator(isIPV6Format: true) },
            { "host-name", new HostnameValidator(isIDNFormat: false) },
            { "uri", new UriValidator(iriSupport: false) },
            { "regex", new RegexValidator() },
            { "date", new DateValidator() },
            { "time", new TimeValidator() },
            { "color", new ColorValidator() },
            { "unknown", new UnknownValidator() },
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

            // In Draft 3, format assertion is controlled by the Draft3 FormatAssertionEnabled option.
            // (Draft 3 does not have vocabulary support.)
            if (!_options.Draft3.FormatAssertionEnabled)
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
