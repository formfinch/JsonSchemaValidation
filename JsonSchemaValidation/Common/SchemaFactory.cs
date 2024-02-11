using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

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
                return schemaData;
            }

            if (!Uri.TryCreate(schemaData.SchemaUri, reference!, out Uri? referenceUri))
            {
                referenceUri = new Uri(reference);
            }

            if (schemaData.References.Contains(referenceUri))
            {
                throw new InvalidSchemaException($"Schema contains cyclic reference: {reference}");
            }
            schemaData.References.Add(referenceUri);

            var retrievedSchema = _schemaRepository.GetSchema(referenceUri);
            foreach(var refUri in schemaData.References)
            {
                retrievedSchema.References.Add(refUri);
            }

            // Dereference until we receive a proper schema.
            return CreateDereferencedSchema(retrievedSchema);
        }
    }
}
