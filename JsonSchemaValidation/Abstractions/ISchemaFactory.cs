using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    public interface ISchemaFactory
    {
        SchemaMetadata CreateDereferencedSchema(SchemaMetadata schemaData);
    }
}
