using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidation.CompiledMetaschemas;

/// <summary>
/// Extension methods to register compiled metaschema validators.
/// </summary>
public static class CompiledMetaschemaSetup
{
    /// <summary>
    /// Adds pre-compiled validators for JSON Schema metaschemas.
    /// This provides faster schema validation as the metaschemas don't need
    /// to be dynamically parsed and built into validators.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompiledMetaschemas(this IServiceCollection services)
    {
        // Compiled metaschema validators will be added here
        // For now, return empty as the generated validators don't exist yet
        var validators = new List<ICompiledValidator>();

        // TODO: Add generated validators when available:
        // validators.Add(new Generated.Draft202012.SchemaValidator());
        // validators.Add(new Generated.Draft201909.SchemaValidator());
        // validators.Add(new Generated.Draft7.SchemaValidator());
        // etc.

        if (validators.Count > 0)
        {
            services.AddCompiledValidators(validators);
        }

        return services;
    }
}
