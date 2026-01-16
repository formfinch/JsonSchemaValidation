using JsonSchemaValidation.CompiledValidators;

namespace JsonSchemaValidation.Abstractions;

/// <summary>
/// A compiled validator that can resolve external $ref dependencies from a registry.
/// Extends <see cref="ICompiledValidator"/> with initialization support for external references.
/// </summary>
/// <remarks>
/// Validators implementing this interface must have their <see cref="Initialize"/> method called
/// before use. The method resolves all external $ref dependencies from the provided registry
/// and caches them for use during validation.
/// </remarks>
public interface IRegistryAwareCompiledValidator : ICompiledValidator
{
    /// <summary>
    /// Resolves all external $ref dependencies from the registry.
    /// This method must be called once before the validator is used.
    /// </summary>
    /// <param name="registry">The registry containing compiled validators for external schemas.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any external $ref cannot be resolved from the registry.
    /// </exception>
    void Initialize(ICompiledValidatorRegistry registry);
}
