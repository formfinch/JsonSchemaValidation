using JsonSchemaValidation.Abstractions.Keywords;

namespace JsonSchemaValidation.Draft202012.Interfaces
{
    public interface ISchemaDraftKeywordValidatorFactory : IKeywordValidatorFactory
    {
        // Interface is used to isolate the schema draft version from other draft implementations.
        
        // Draft 2020-12 specific methods or properties here
    }
}
