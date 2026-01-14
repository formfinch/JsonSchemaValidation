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
        ICompiledValidator[] validators =
        [
            // Draft 2020-12
            new CompiledValidator_Draft202012Schema(),
            new CompiledValidator_Draft202012MetaCore(),
            new CompiledValidator_Draft202012MetaApplicator(),
            new CompiledValidator_Draft202012MetaValidation(),
            new CompiledValidator_Draft202012MetaMetaData(),
            new CompiledValidator_Draft202012MetaFormatAnnotation(),
            new CompiledValidator_Draft202012MetaContent(),
            new CompiledValidator_Draft202012MetaUnevaluated(),

            // Draft 2019-09
            new CompiledValidator_Draft201909Schema(),
            new CompiledValidator_Draft201909MetaCore(),
            new CompiledValidator_Draft201909MetaApplicator(),
            new CompiledValidator_Draft201909MetaValidation(),
            new CompiledValidator_Draft201909MetaMetaData(),
            new CompiledValidator_Draft201909MetaFormat(),
            new CompiledValidator_Draft201909MetaContent(),

            // Draft 7
            new CompiledValidator_Draft7Schema(),

            // Draft 6
            new CompiledValidator_Draft6Schema(),

            // Draft 4
            new CompiledValidator_Draft4Schema(),

            // Draft 3
            new CompiledValidator_Draft3Schema()
        ];

        services.AddCompiledValidators(validators);

        return services;
    }
}
