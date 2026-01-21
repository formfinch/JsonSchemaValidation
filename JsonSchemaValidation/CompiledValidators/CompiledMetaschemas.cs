using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.CompiledValidators.Generated;

namespace FormFinch.JsonSchemaValidation.CompiledValidators;

/// <summary>
/// Provides access to all pre-compiled metaschema validators.
/// </summary>
public static class CompiledMetaschemas
{
    /// <summary>
    /// Gets all compiled metaschema validators for registration.
    /// </summary>
    public static ICompiledValidator[] GetAll() =>
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
}
