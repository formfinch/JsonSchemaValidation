using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidation.CompiledMetaschemas;

/// <summary>
/// Extension methods to register compiled metaschema validators.
/// </summary>
[Obsolete("Compiled metaschemas are now loaded by default. This method is no longer needed.")]
public static class CompiledMetaschemaSetup
{
    /// <summary>
    /// Adds pre-compiled validators for JSON Schema metaschemas.
    /// </summary>
    /// <remarks>
    /// This method is now a no-op. Compiled metaschema validators are automatically
    /// registered when calling <c>AddJsonSchemaValidation()</c>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete("Compiled metaschemas are now loaded by default. This method is no longer needed.")]
    public static IServiceCollection AddCompiledMetaschemas(this IServiceCollection services)
    {
        // No-op: Compiled metaschemas are now loaded by default in the main library.
        return services;
    }
}
