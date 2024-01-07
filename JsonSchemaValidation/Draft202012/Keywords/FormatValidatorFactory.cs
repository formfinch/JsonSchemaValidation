using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Draft202012.Keywords.Format;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class FormatValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
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

            return null;
        }
    }
}
