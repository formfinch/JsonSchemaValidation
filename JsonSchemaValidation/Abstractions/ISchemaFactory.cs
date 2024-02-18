using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaFactory
    {
        SchemaMetadata CreateDereferencedSchema(SchemaMetadata schemaData);
    }
}