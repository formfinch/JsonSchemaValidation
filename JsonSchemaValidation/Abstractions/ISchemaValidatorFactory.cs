using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaValidatorFactory
    {
        ISchemaValidator GetValidator(Uri schemaUri);
        ISchemaValidator CreateValidator(SchemaMetadata schemaMetaData);
    }
}
