using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Common
{
    public class SchemaFactory : ISchemaFactory
    {
        private static readonly Uri NopSchemaUri = new("http://formfinch.com/jsonschemavalidation/nop-true");

        private readonly ISchemaRepository _schemaRepository;
        private readonly Lazy<SchemaMetadata> _nopSchema;

        public SchemaFactory(ISchemaRepository schemaRepository)
        {
            _schemaRepository = schemaRepository;

            _nopSchema = new Lazy<SchemaMetadata>(() =>
            {
                return _schemaRepository.GetSchema(NopSchemaUri);
            });
        }

        public SchemaMetadata NopSchema => _nopSchema.Value;

        public SchemaMetadata CreateDereferencedSchema(SchemaMetadata schemaData)
        {
            // In Draft 2020-12, $ref and $dynamicRef are applicators that work alongside
            // sibling keywords. They are NOT dereferenced here - they are handled by
            // RefValidator and DynamicRefValidator at validation time.
            // This allows schemas like { "$ref": "other.json", "unevaluatedProperties": false }
            // to work correctly with both $ref and sibling keywords applied.
            return schemaData;
        }
    }
}
