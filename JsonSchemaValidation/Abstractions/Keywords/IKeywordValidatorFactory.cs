using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Abstractions.Keywords
{
    public interface IKeywordValidatorFactory
    {
        IKeywordValidator? Create(SchemaMetadata schemaData);
    }
}
