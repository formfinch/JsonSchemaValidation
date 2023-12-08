using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Common
{
    public class SchemaFactory : ISchemaFactory
    {
        private readonly ISchemaRepository _schemaRepository;

        public SchemaFactory(ISchemaRepository schemaRepository)
        {
            _schemaRepository = schemaRepository;
        }

        public SchemaMetadata CreateDereferencedSchema(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;
            if(schema.ValueKind != JsonValueKind.Object)
            {
                return schemaData;
            }

            // check for $ref
            if (!schema.TryGetProperty("$ref", out JsonElement refElement))
            {
                return schemaData;
            }

            if (refElement.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("$ref not a string value");
            }

            string reference = refElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reference) || reference == "#")
            {
                // todo: empty string and "#" are different things.
                // empty string should return the current schemaData
                // "#" and schemaData.uri.fragment
                // should return _schemaRepository.GetSchema(
                return schemaData;
            }

            Uri referenceUri = reference.StartsWith("#") ? new Uri(schemaData.SchemaUri!, new Uri(reference, UriKind.Relative)) : new Uri(reference);
            if (schemaData.References.Contains(referenceUri))
            {
                throw new InvalidSchemaException($"Schema contains cyclic reference: {reference}");
            }
            var retrievedSchema = _schemaRepository.GetSchema(referenceUri);
            retrievedSchema.References.Add(referenceUri);

            // Dereference until we receive a proper schema.
            return CreateDereferencedSchema(retrievedSchema);
        }
    }
}
