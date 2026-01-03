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

        /// <summary>
        /// Determines the order in which validators are executed.
        /// Lower values execute first. Default is 0.
        /// Unevaluated keywords (unevaluatedItems, unevaluatedProperties) should use higher values
        /// to ensure they run after other applicator keywords.
        /// </summary>
        int ExecutionOrder => 0;
    }
}
