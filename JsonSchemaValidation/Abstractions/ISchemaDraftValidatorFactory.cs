using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    public interface ISchemaDraftValidatorFactory
    {
        string DraftVersion { get; }
        ISchemaValidator CreateValidator(SchemaMetadata schemaData);
    }
}
