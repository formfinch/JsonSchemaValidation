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
    /// Function to resolve a local $ref (e.g., "#/$defs/foo") to the target schema.
    /// Returns null if the reference cannot be resolved.
    /// </summary>
    public required Func<string, JsonElement?> ResolveLocalRef { get; init; }

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
    /// The JS expression for the element being validated (usually "v").
    /// </summary>
    public string ElementExpr { get; init; } = "v";

    /// <summary>
    /// Generates a call to validate a subschema with the current element expression.
    /// </summary>
    public string GenerateValidateCall(string hash)
    {
        return $"validate_{hash}({ElementExpr})";
    }

    /// <summary>
    /// Generates a call to validate a subschema with a different element expression.
    /// </summary>
    public string GenerateValidateCallForExpr(string hash, string expr)
    {
        return $"validate_{hash}({expr})";
    }
}
