// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Interface for keyword-specific JavaScript code generators.
/// Parallel to IKeywordCodeGenerator but emits ESM JavaScript instead of C#.
/// </summary>
public interface IJsKeywordCodeGenerator
{
    /// <summary>
    /// The JSON Schema keyword this generator handles.
    /// </summary>
    string Keyword { get; }

    /// <summary>
    /// Priority for code generation. Higher values run first.
    /// Type checks should run first (100), then constraints (50), then applicators (0).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Returns true if this generator can handle the keyword in the given schema.
    /// </summary>
    bool CanGenerate(JsonElement schema);

    /// <summary>
    /// Generates validation code for the keyword.
    /// </summary>
    string GenerateCode(JsCodeGenerationContext context);

    /// <summary>
    /// Returns any helper imports required from the runtime module.
    /// Consumed by the orchestrator to build a single deduplicated import statement.
    /// </summary>
    IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context);
}

/// <summary>
/// Context provided to JavaScript keyword code generators.
/// </summary>
public sealed class JsCodeGenerationContext
{
    /// <summary>
    /// The current schema being processed.
    /// </summary>
    public required JsonElement CurrentSchema { get; init; }

    /// <summary>
    /// The resource-root hash for the current schema.
    /// </summary>
    public string CurrentResourceRootHash { get; init; } = "";

    /// <summary>
    /// The hash of the current schema.
    /// </summary>
    public required string CurrentHash { get; init; }

    /// <summary>
    /// Function to get the hash for a subschema.
    /// </summary>
    public required Func<JsonElement, string> GetSubschemaHash { get; init; }

    /// <summary>
    /// Function to resolve a local $ref (e.g., "#/$defs/foo") to the target schema,
    /// against the document root. Use ResolveLocalRefInResource when the current
    /// subschema is inside a nested $id resource.
    /// </summary>
    public required Func<string, JsonElement?> ResolveLocalRef { get; init; }

    /// <summary>
    /// Function to resolve a URI to an internal schema that has a matching $id.
    /// </summary>
    public required Func<string, JsonElement?> ResolveInternalId { get; init; }

    /// <summary>
    /// Function to resolve a local $ref within a specific schema resource.
    /// Required for correct fragment resolution inside nested $id boundaries.
    /// </summary>
    public required Func<string, JsonElement, JsonElement?> ResolveLocalRefInResource { get; init; }

    /// <summary>
    /// The schema resource root for this subschema: the nearest ancestor with $id
    /// (or id for Draft 4), or the document root if none. Used to scope local ref
    /// resolution.
    /// </summary>
    public JsonElement? ResourceRoot { get; init; }

    /// <summary>
    /// The effective base URI for this subschema.
    /// </summary>
    public Uri? BaseUri { get; init; }

    /// <summary>
    /// The root schema base URI.
    /// </summary>
    public Uri? RootBaseUri { get; init; }

    /// <summary>
    /// Function to resolve subschema metadata by hash.
    /// </summary>
    public required Func<string, SubschemaInfo?> GetSubschemaInfo { get; init; }

    /// <summary>
    /// The detected JSON Schema draft version for this schema.
    /// Used to honor draft-specific keyword behavior.
    /// </summary>
    public SchemaDraft DetectedDraft { get; init; } = SchemaDraft.Draft202012;

    /// <summary>
    /// Whether Draft 2020-12 should assert supported "format" values. Earlier
    /// supported drafts still assert format by default.
    /// </summary>
    public bool FormatAssertionEnabled { get; init; }

    /// <summary>
    /// Whether the active metaschema enables the validation vocabulary.
    /// When false, validation-vocabulary keywords must be treated as unknown.
    /// </summary>
    public bool ValidationVocabularyEnabled { get; init; } = true;

    /// <summary>
    /// Whether generated validators need a registry parameter for external refs.
    /// </summary>
    public bool RequiresRegistry { get; init; }

    /// <summary>
    /// Whether generated validators need dynamic scope propagation for $dynamicRef.
    /// </summary>
    public bool RequiresScopeTracking { get; init; }

    /// <summary>
    /// The JS expression for the registry object.
    /// </summary>
    public string RegistryExpr { get; init; } = "_registry";

    /// <summary>
    /// The JS expression for the current dynamic scope stack.
    /// </summary>
    public string ScopeExpr { get; init; } = "_scope";

    /// <summary>
    /// Whether the schema tree contains unevaluatedProperties.
    /// When true, property evaluation must be tracked.
    /// </summary>
    public bool RequiresPropertyAnnotations { get; init; }

    /// <summary>
    /// Whether the schema tree contains unevaluatedItems.
    /// When true, item evaluation must be tracked.
    /// </summary>
    public bool RequiresItemAnnotations { get; init; }

    /// <summary>
    /// Whether annotation tracking requires passing state/location through calls.
    /// </summary>
    public bool RequiresAnnotationTracking => RequiresPropertyAnnotations || RequiresItemAnnotations;

    /// <summary>
    /// The JS expression for the evaluated-state object.
    /// </summary>
    public string EvaluatedStateExpr { get; init; } = "_eval";

    /// <summary>
    /// The JS expression for the current instance location.
    /// </summary>
    public string LocationExpr { get; init; } = "_loc";

    /// <summary>
    /// The JS expression for the element being validated (usually "v").
    /// </summary>
    public string ElementExpr { get; init; } = "v";

    /// <summary>
    /// Generates a call to validate a subschema with the current element expression.
    /// </summary>
    public string GenerateValidateCall(string hash)
    {
        return BuildValidateCall(hash, ElementExpr, LocationExpr);
    }

    public string GenerateValidateCall(JsonElement schema)
    {
        return BuildValidateCall(schema, ElementExpr, LocationExpr);
    }

    /// <summary>
    /// Generates a call to validate a subschema with a different element expression.
    /// </summary>
    public string GenerateValidateCallForExpr(string hash, string expr)
    {
        return BuildValidateCall(hash, expr, LocationExpr);
    }

    public string GenerateValidateCallForExpr(JsonElement schema, string expr)
    {
        return BuildValidateCall(schema, expr, LocationExpr);
    }

    /// <summary>
    /// Generates a call to validate a property value and pushes the property onto the instance location.
    /// </summary>
    public string GenerateValidateCallForProperty(string hash, string expr, string propertyNameExpr)
    {
        var loc = RequiresAnnotationTracking
            ? $"{LocationExpr} + \"/\" + escapeJsonPointer({propertyNameExpr})"
            : LocationExpr;
        return BuildValidateCall(hash, expr, loc);
    }

    public string GenerateValidateCallForProperty(JsonElement schema, string expr, string propertyNameExpr)
    {
        var loc = RequiresAnnotationTracking
            ? $"{LocationExpr} + \"/\" + escapeJsonPointer({propertyNameExpr})"
            : LocationExpr;
        return BuildValidateCall(schema, expr, loc);
    }

    /// <summary>
    /// Generates a call to validate an array item and pushes the index onto the instance location.
    /// </summary>
    public string GenerateValidateCallForItem(string hash, string expr, string indexExpr)
    {
        var loc = RequiresAnnotationTracking
            ? $"{LocationExpr} + \"/\" + {indexExpr}"
            : LocationExpr;
        return BuildValidateCall(hash, expr, loc);
    }

    public string GenerateValidateCallForItem(JsonElement schema, string expr, string indexExpr)
    {
        var loc = RequiresAnnotationTracking
            ? $"{LocationExpr} + \"/\" + {indexExpr}"
            : LocationExpr;
        return BuildValidateCall(schema, expr, loc);
    }

    private string BuildValidateCall(JsonElement schema, string expr, string locExpr)
    {
        if (!IsInlineableRefSchema(schema))
        {
            return BuildValidateCall(GetSubschemaHash(schema), expr, locExpr);
        }

        var inlineContext = CreateChildContext(schema, expr, locExpr);
        var code = schema.TryGetProperty("$ref", out _)
            ? new JsRefCodeGenerator().GenerateCode(inlineContext)
            : new JsDynamicRefCodeGenerator().GenerateCode(inlineContext);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("(() => {");
        foreach (var line in code.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            sb.Append("  ");
            sb.AppendLine(line.TrimEnd());
        }
        sb.Append("  return true;\n})()");
        return sb.ToString();
    }

    private string BuildValidateCall(string hash, string expr, string locExpr)
    {
        var args = new List<string> { expr };
        if (RequiresScopeTracking)
        {
            args.Add(ScopeExpr);
        }
        if (RequiresAnnotationTracking)
        {
            args.Add(EvaluatedStateExpr);
        }
        if (RequiresScopeTracking || RequiresAnnotationTracking)
        {
            args.Add(locExpr);
        }
        if (RequiresRegistry)
        {
            args.Add(RegistryExpr);
        }
        return $"validate_{hash}({string.Join(", ", args)})";
    }

    private JsCodeGenerationContext CreateChildContext(JsonElement schema, string expr, string locExpr)
    {
        return new JsCodeGenerationContext
        {
            CurrentSchema = schema,
            CurrentResourceRootHash = CurrentResourceRootHash,
            CurrentHash = GetSubschemaHash(schema),
            GetSubschemaHash = GetSubschemaHash,
            ResolveLocalRef = ResolveLocalRef,
            ResolveInternalId = ResolveInternalId,
            ResolveLocalRefInResource = ResolveLocalRefInResource,
            ResourceRoot = ResourceRoot,
            BaseUri = BaseUri,
            RootBaseUri = RootBaseUri,
            GetSubschemaInfo = GetSubschemaInfo,
            DetectedDraft = DetectedDraft,
            FormatAssertionEnabled = FormatAssertionEnabled,
            ValidationVocabularyEnabled = ValidationVocabularyEnabled,
            RequiresRegistry = RequiresRegistry,
            RequiresScopeTracking = RequiresScopeTracking,
            RegistryExpr = RegistryExpr,
            ScopeExpr = ScopeExpr,
            RequiresPropertyAnnotations = RequiresPropertyAnnotations,
            RequiresItemAnnotations = RequiresItemAnnotations,
            EvaluatedStateExpr = EvaluatedStateExpr,
            LocationExpr = locExpr,
            ElementExpr = expr
        };
    }

    private static bool IsInlineableRefSchema(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var count = 0;
        var hasInlineableKeyword = false;
        foreach (var property in schema.EnumerateObject())
        {
            count++;
            if ((property.Name == "$ref" || property.Name == "$dynamicRef") &&
                property.Value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(property.Value.GetString()))
            {
                hasInlineableKeyword = true;
                continue;
            }

            return false;
        }

        return hasInlineableKeyword && count == 1;
    }
}
