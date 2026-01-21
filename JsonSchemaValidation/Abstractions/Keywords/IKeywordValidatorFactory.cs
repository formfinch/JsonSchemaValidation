using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Abstractions.Keywords
{
    public interface IKeywordValidatorFactory
    {
        IKeywordValidator? Create(SchemaMetadata schemaData);
    }
}
