using JsonSchemaValidation.Abstractions.Keywords;

namespace JsonSchemaValidation.Draft202012.Interfaces
{
    /// <summary>
    /// Factory interface for Draft 2020-12 keyword validators.
    /// Extends IKeywordValidatorFactory with vocabulary support.
    /// </summary>
    public interface ISchemaDraftKeywordValidatorFactory : IKeywordValidatorFactory
    {
        /// <summary>
        /// The keyword name this factory handles (e.g., "minimum", "properties").
        /// Used for vocabulary-based filtering.
        /// </summary>
        string Keyword { get; }
    }
}
