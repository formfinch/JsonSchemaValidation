using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Draft202012.Keywords.Format;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class FormatValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly SchemaValidationOptions _options;

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
            if(string.IsNullOrEmpty(format))
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

            if(format == "date-time")
            {
                return new DateTimeValidator();
            }

            if (format == "date")
            {
                return new DateValidator();
            }

            if (format == "duration")
            {
                return new DurationValidator();
            }

            if (format == "email")
            {
                return new EmailValidator();
            }

            if (format == "hostname")
            {
                return new HostnameValidator(isIDNFormat: false);
            }

            if (format == "idn-email")
            {
                return new EmailValidator();
            }

            if (format == "idn-hostname")
            {
                return new HostnameValidator(isIDNFormat:true);
            }

            if (format == "ipv4")
            {
                return new IPAddressValidator(isIPV6Format:false);
            }

            if (format == "ipv6")
            {
                return new IPAddressValidator(isIPV6Format: true);
            }

            if (format == "iri")
            {
                return new UriValidator(iriSupport: true);
            }

            if (format == "iri-reference")
            {
                return new UriValidator(iriSupport: true, canBeRelative: true);
            }

            if (format == "json-pointer")
            {
                return new JsonPointerValidator();
            }

            if (format == "regex")
            {
                return new RegexValidator();
            }

            if (format == "relative-json-pointer")
            {
                return new RelativeJsonPointerValidator();
            }

            if (format == "time")
            {
                return new TimeValidator();
            }

            if (format == "unknown")
            {
                return new UnknownValidator();
            }

            if (format == "uri")
            {
                return new UriValidator(iriSupport: false);
            }

            if (format == "uri-reference")
            {
                return new UriValidator(iriSupport: false, canBeRelative: true);
            }

            if (format == "uri-template")
            {
                return new UriValidator(isTemplate: true);
            }

            if (format == "uuid")
            {
                return new UuidValidator();
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
                if (schemaData.ActiveVocabularies.TryGetValue(formatAssertionVocabulary, out var isRequired))
                {
                    // The vocabulary is present, which means format assertion is active
                    return true;
                }
            }

            return false;
        }
    }
}
