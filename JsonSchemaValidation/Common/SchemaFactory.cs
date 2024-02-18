using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System.Text.Json;
using System.Linq;

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

        public SchemaMetadata CreateDereferencedSchema(SchemaMetadata schemaData, IList<SchemaMetadata>? scope = null)
        {
            var schema = schemaData.Schema;
            if(schema.ValueKind != JsonValueKind.Object)
            {
                return schemaData;
            }

            if(scope == null)
            {
                scope = new List<SchemaMetadata>();
            }

            // check for $ref
            var hasRef = schema.TryGetProperty("$ref", out JsonElement refElement);
            var hasDynamicRef = schema.TryGetProperty("$dynamicRef", out JsonElement dynamicRefElement);

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

                List<SchemaMetadata> nextScope = new List<SchemaMetadata>(scope);
                nextScope.Add(schemaData);

                // Dereference until we receive a proper schema.
                return CreateDereferencedSchema(retrievedSchema, nextScope);
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

                var resultInScope = scope.FirstOrDefault(s => s.DynamicAnchors.ContainsKey(reference));
                if(resultInScope != null)
                {
                    if (!Uri.TryCreate(resultInScope.SchemaUri, reference, out Uri? dynamicRefUri))
                    {
                        throw new InvalidOperationException("Failed to register visited reference");
                    }

                    if (schemaData.References.TryGetValue(dynamicRefUri, out var previouslyDynamicallyVisited))
                    {
                        if (++previouslyDynamicallyVisited.CyclicReference < 2)
                        {
                            // no dereference, just return it
                            return previouslyDynamicallyVisited;
                        }
                        else
                        {
                            // nesting too deep
                            return NopSchema;
                        }
                    }
                    else
                    {
                        // create a new copy of currentSchema
                        SchemaMetadata nextSchemadata = new(resultInScope)
                        {
                            SchemaUri = dynamicRefUri,
                        };

                        List<SchemaMetadata> nextScope = new List<SchemaMetadata>(scope);
                        nextScope.Add(schemaData);
                        // Dereference until we receive a proper schema.
                        return CreateDereferencedSchema(nextSchemadata, nextScope);
                    }
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

                    if (schemaData.References.TryGetValue(dynamicRefUri, out var previouslyDynamicallyVisited))
                    {
                        if (++previouslyDynamicallyVisited.CyclicReference < 2)
                        {
                            // no dereference, just return it
                            return previouslyDynamicallyVisited;
                        }
                        else
                        {
                            // nesting too deep
                            return NopSchema;
                        }
                    }
                    else
                    {
                        schemaData.References.TryAdd(dynamicRefUri, currentSchema);
                        foreach (var visitedReference in schemaData.References)
                        {
                            currentSchema.References.TryAdd(visitedReference.Key, visitedReference.Value);
                        }

                        // Dereference until we receive a proper schema.
                        return CreateDereferencedSchema(currentSchema);
                    }
                }


                if (!Uri.TryCreate(schemaData.SchemaUri, reference!, out Uri? referenceUri))
                {
                    referenceUri = new Uri(reference);
                }

                if (schemaData.References.ContainsKey(referenceUri))
                {
                    // cyclic reference, stop dereferencing
                    // throw new InvalidSchemaException($"Schema contains cyclic reference: {reference}");
                    if (schemaData.References.TryGetValue(referenceUri, out var previouslyVisited))
                    {
                        if (++previouslyVisited.CyclicReference < 2)
                        {
                            // no dereference, just return it
                            return previouslyVisited;
                        }
                        else
                        {
                            // stop dereferencing
                            return NopSchema;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to retrieve schema");
                    }
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
