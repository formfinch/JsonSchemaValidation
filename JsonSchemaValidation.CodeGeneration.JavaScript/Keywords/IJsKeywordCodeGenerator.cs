// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

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
    /// Function to resolve subschema metadata by hash.
    /// </summary>
    public required Func<string, SubschemaInfo?> GetSubschemaInfo { get; init; }

    /// <summary>
    /// The detected JSON Schema draft version for this schema.
    /// Used to honor draft-specific keyword behavior.
    /// </summary>
    public SchemaDraft DetectedDraft { get; init; } = SchemaDraft.Draft202012;

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
        return RequiresAnnotationTracking
            ? $"validate_{hash}({ElementExpr}, {EvaluatedStateExpr}, {LocationExpr})"
            : $"validate_{hash}({ElementExpr})";
    }

    /// <summary>
    /// Generates a call to validate a subschema with a different element expression.
    /// </summary>
    public string GenerateValidateCallForExpr(string hash, string expr)
    {
        return RequiresAnnotationTracking
            ? $"validate_{hash}({expr}, {EvaluatedStateExpr}, {LocationExpr})"
            : $"validate_{hash}({expr})";
    }

    /// <summary>
    /// Generates a call to validate a property value and pushes the property onto the instance location.
    /// </summary>
    public string GenerateValidateCallForProperty(string hash, string expr, string propertyNameExpr)
    {
        return RequiresAnnotationTracking
            ? $"validate_{hash}({expr}, {EvaluatedStateExpr}, {LocationExpr} + \"/\" + escapeJsonPointer({propertyNameExpr}))"
            : $"validate_{hash}({expr})";
    }

    /// <summary>
    /// Generates a call to validate an array item and pushes the index onto the instance location.
    /// </summary>
    public string GenerateValidateCallForItem(string hash, string expr, string indexExpr)
    {
        return RequiresAnnotationTracking
            ? $"validate_{hash}({expr}, {EvaluatedStateExpr}, {LocationExpr} + \"/\" + {indexExpr})"
            : $"validate_{hash}({expr})";
    }
}
