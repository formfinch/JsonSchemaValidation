using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    public interface ISchemaValidatorFactory
    {
        ISchemaValidator GetValidator(Uri schemaUri);
        ISchemaValidator CreateValidator(SchemaMetadata schemaMetaData);
    }
}
