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

            if (format == "idn-email")
            {
                return new EmailValidator();
            }

            if (format == "time")
            {
                return new TimeValidator();
            }

            return null;
        }
    }
}
