namespace FormFinch.JsonSchemaValidation.CodeGenerator;

/// <summary>
/// Defines all metaschemas that should be compiled.
/// </summary>
internal static class MetaschemaDefinitions
{
    public static readonly MetaschemaInfo[] Schemas =
    [
        // Draft 2020-12
        new("Draft202012", "json-schema-draft202012-schema.json", "Draft202012Schema"),
        new("Draft202012", "json-schema-draft202012-meta-core.json", "Draft202012MetaCore"),
        new("Draft202012", "json-schema-draft202012-meta-applicator.json", "Draft202012MetaApplicator"),
        new("Draft202012", "json-schema-draft202012-meta-validation.json", "Draft202012MetaValidation"),
        new("Draft202012", "json-schema-draft202012-meta-meta-data.json", "Draft202012MetaMetaData"),
        new("Draft202012", "json-schema-draft202012-meta-format-annotation.json", "Draft202012MetaFormatAnnotation"),
        new("Draft202012", "json-schema-draft202012-meta-content.json", "Draft202012MetaContent"),
        new("Draft202012", "json-schema-draft202012-meta-unevaluated.json", "Draft202012MetaUnevaluated"),

        // Draft 2019-09
        new("Draft201909", "json-schema-draft201909-schema.json", "Draft201909Schema"),
        new("Draft201909", "json-schema-draft201909-meta-core.json", "Draft201909MetaCore"),
        new("Draft201909", "json-schema-draft201909-meta-applicator.json", "Draft201909MetaApplicator"),
        new("Draft201909", "json-schema-draft201909-meta-validation.json", "Draft201909MetaValidation"),
        new("Draft201909", "json-schema-draft201909-meta-meta-data.json", "Draft201909MetaMetaData"),
        new("Draft201909", "json-schema-draft201909-meta-format.json", "Draft201909MetaFormat"),
        new("Draft201909", "json-schema-draft201909-meta-content.json", "Draft201909MetaContent"),

        // Draft 7
        new("Draft7", "json-schema-draft7-schema.json", "Draft7Schema"),

        // Draft 6
        new("Draft6", "json-schema-draft6-schema.json", "Draft6Schema"),

        // Draft 4
        new("Draft4", "json-schema-draft4-schema.json", "Draft4Schema"),

        // Draft 3
        new("Draft3", "json-schema-draft3-schema.json", "Draft3Schema")
    ];
}

internal sealed record MetaschemaInfo(string DraftFolder, string FileName, string ClassName);
