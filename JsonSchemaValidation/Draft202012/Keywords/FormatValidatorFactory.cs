using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Draft202012.Keywords.Format;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class FormatValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly SchemaValidationOptions _options;

        /// <summary>
        /// Cached format assertion validators. These are stateless and can be safely shared.
        /// </summary>
        private static readonly Dictionary<string, IKeywordValidator> CachedAssertionValidators = new(StringComparer.Ordinal)
        {
            { "date-time", new DateTimeValidator() },
            { "date", new DateValidator() },
            { "duration", new DurationValidator() },
            { "email", new EmailValidator() },
            { "hostname", new HostnameValidator(isIDNFormat: false) },
            { "idn-email", new EmailValidator() },
            { "idn-hostname", new HostnameValidator(isIDNFormat: true) },
            { "ipv4", new IPAddressValidator(isIPV6Format: false) },
            { "ipv6", new IPAddressValidator(isIPV6Format: true) },
            { "iri", new UriValidator(iriSupport: true) },
            { "iri-reference", new UriValidator(iriSupport: true, canBeRelative: true) },
            { "json-pointer", new JsonPointerValidator() },
            { "regex", new RegexValidator() },
            { "relative-json-pointer", new RelativeJsonPointerValidator() },
            { "time", new TimeValidator() },
            { "unknown", new UnknownValidator() },
            { "uri", new UriValidator(iriSupport: false) },
            { "uri-reference", new UriValidator(iriSupport: false, canBeRelative: true) },
            { "uri-template", new UriValidator(isTemplate: true) },
            { "uuid", new UuidValidator() },
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

            // Check if format-assertion vocabulary is active for this schema
            bool formatAssertionActive = IsFormatAssertionActive(schemaData);

            // If format assertion is not active, return annotation-only validator
            if (!formatAssertionActive)
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

        /// <summary>
        /// Determines if format assertion is active for the given schema.
        /// Format assertion is active if:
        /// 1. The global FormatAssertionEnabled option is true, OR
        /// 2. The schema's metaschema includes the format-assertion vocabulary with value true
        /// </summary>
        private bool IsFormatAssertionActive(SchemaMetadata schemaData)
        {
            // Check global option first
            if (_options.FormatAssertionEnabled)
            {
                return true;
            }

            // Check if schema has format-assertion vocabulary enabled
            if (schemaData.ActiveVocabularies != null)
            {
                const string formatAssertionVocabulary = "https://json-schema.org/draft/2020-12/vocab/format-assertion";
                if (schemaData.ActiveVocabularies.ContainsKey(formatAssertionVocabulary))
                {
                    // The vocabulary is present, which means format assertion is active
                    return true;
                }
            }

            return false;
        }
    }
}
