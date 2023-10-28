using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaRepository
    {
        SchemaMetadata AddSchema(SchemaMetadata schemaData);
        SchemaMetadata GetSchema(Uri schemaUri);
    }
}