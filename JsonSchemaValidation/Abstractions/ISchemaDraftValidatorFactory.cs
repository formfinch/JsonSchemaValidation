using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaDraftValidatorFactory
    {
        string DraftVersion { get; }
        ISchemaValidator CreateValidator(SchemaMetadata schemaData);
    }
}
