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
            JsonElement dynamicRefElement = default;
            var hasRef = schema.TryGetProperty("$ref", out JsonElement refElement);
            var hasDynamicRef = !hasRef && schema.TryGetProperty("$dynamicRef", out dynamicRefElement);

            if (!hasRef && !hasDynamicRef)
            {
                return schemaData;
            }

            if (hasRef)
            {
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

            if(hasDynamicRef)
            {
                if (dynamicRefElement.ValueKind != JsonValueKind.String)
                {
                    throw new FormatException("$dynamicRef not a string value");
                }

                string reference = dynamicRefElement.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(reference) || reference == "#")
                {
                    return schemaData;
                }

                // was the dynamicRef registered previously 
                // then get the first occurence as a fresh schema reference
                if (_schemaRepository.TryGetDynamicRef(reference, out SchemaMetadata? dynamicRetrievedSchema))
                {
                    SchemaMetadata currentSchema = dynamicRetrievedSchema!;
                    if (!Uri.TryCreate(currentSchema.SchemaUri, reference, out Uri? dynamicRefUri))
                    {
                        throw new InvalidOperationException("Failed to register cyclic reference");
                    }

                    if (!schemaData.References.TryGetValue(dynamicRefUri, out _))
                    {
                        schemaData.References.TryAdd(dynamicRefUri, currentSchema);
                        foreach (var visitedReference in schemaData.References)
                        {
                            currentSchema.References.TryAdd(visitedReference.Key, visitedReference.Value);
                        }

                        // Dereference until we receive a proper schema.
                        return CreateDereferencedSchema(currentSchema);
                    }

                    return NopSchema;
                }


                if (!Uri.TryCreate(schemaData.SchemaUri, reference!, out Uri? referenceUri))
                {
                    referenceUri = new Uri(reference);
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

            throw new InvalidOperationException();
        }
    }
}
