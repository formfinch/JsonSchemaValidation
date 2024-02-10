using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaRepository
    {
        bool TryAddSchema(SchemaMetadata schemaData, out SchemaMetadata? result);
        SchemaMetadata GetSchema(Uri schemaUri);
    }
}