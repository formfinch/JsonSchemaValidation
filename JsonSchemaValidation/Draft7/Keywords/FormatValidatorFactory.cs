// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
// Draft behavior: Similar in Draft 2019-09 and Draft 2020-12 (vocabulary URI differs)
// Note: In Draft 4-7, format was always annotation-only.
// Factory for creating format validators based on format assertion settings.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Draft7.Keywords.Format;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords
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
        /// 2. The schema's metaschema includes the format vocabulary with value true
        /// </summary>
        private bool IsFormatAssertionActive(SchemaMetadata schemaData)
        {
            // Check Draft7-specific option first
            if (_options.Draft7.FormatAssertionEnabled)
            {
                return true;
            }

            // Check if schema has format vocabulary enabled
            if (schemaData.ActiveVocabularies != null)
            {
                // Draft 2019-09 uses a single format vocabulary (not separate annotation/assertion)
                const string formatVocabulary = "https://json-schema.org/draft/2019-09/vocab/format";
                if (schemaData.ActiveVocabularies.ContainsKey(formatVocabulary))
                {
                    // The vocabulary is present, which means format assertion is active
                    return true;
                }
            }

            return false;
        }
    }
}
