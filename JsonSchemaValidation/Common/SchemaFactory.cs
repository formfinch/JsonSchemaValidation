using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Common
{
    public class SchemaFactory : ISchemaFactory
    {
        private readonly ISchemaRepository _schemaRepository;
        private Lazy<SchemaMetadata> _nopSchema;

        public SchemaFactory(ISchemaRepository schemaRepository)
        {
            _schemaRepository = schemaRepository;

            _nopSchema = new Lazy<SchemaMetadata>(() =>
            {
                var nopUri = new Uri("http://formfinch.com/jsonschemavalidation/nop-true");
                return _schemaRepository.GetSchema(nopUri);
            });
        }

        public SchemaMetadata NopSchema => _nopSchema.Value;

        public SchemaMetadata CreateDereferencedSchema(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;
            if(schema.ValueKind != JsonValueKind.Object)
            {
                return schemaData;
            }

            // check for $ref
            // Note: $dynamicRef is NOT dereferenced here - it's handled by DynamicRefValidator
            // at validation time when the dynamic scope is available
            var hasRef = schema.TryGetProperty("$ref", out JsonElement refElement);

            if (!hasRef)
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

            if (schemaData.References.ContainsKey(referenceUri))
            {
                // cyclic reference, stop dereferencing
                return NopSchema;
            }
            var retrievedSchema = _schemaRepository.GetSchema(referenceUri, dynamicRef: true);
            schemaData.References.TryAdd(referenceUri, retrievedSchema);
            foreach (var visitedReference in schemaData.References)
            {
                retrievedSchema.References.TryAdd(visitedReference.Key, visitedReference.Value);
            }

            // Dereference until we receive a proper schema.
            return CreateDereferencedSchema(retrievedSchema);
        }
    }
}
