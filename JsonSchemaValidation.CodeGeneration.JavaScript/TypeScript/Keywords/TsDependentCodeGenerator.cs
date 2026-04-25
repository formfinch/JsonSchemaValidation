// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for the "dependentRequired" keyword (Draft 2019-09+).
/// </summary>
public sealed class TsDependentRequiredCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "dependentRequired";
    public int Priority => 55;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("dependentRequired", out var d) &&
        d.ValueKind == JsonValueKind.Object;

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled) return string.Empty;
        if (context.DetectedDraft < SchemaDraft.Draft201909) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("dependentRequired", out var depElem) ||
            depElem.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }
        return EmitDependentRequired(context.ElementExpr, depElem);
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];

    internal static string EmitDependentRequired(string v, JsonElement depElem)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        foreach (var dep in depElem.EnumerateObject())
        {
            if (dep.Value.ValueKind != JsonValueKind.Array) continue;
            var trigger = TsLiteral.String(dep.Name);
            sb.AppendLine($"  if (Object.prototype.hasOwnProperty.call({v}, {trigger})) {{");
            foreach (var required in dep.Value.EnumerateArray())
            {
                if (required.ValueKind != JsonValueKind.String) continue;
                sb.AppendLine($"    if (!Object.prototype.hasOwnProperty.call({v}, {TsLiteral.String(required.GetString()!)})) return false;");
            }
            sb.AppendLine("  }");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }
}

/// <summary>
/// Generates TypeScript code for the "dependentSchemas" keyword (Draft 2019-09+).
/// </summary>
public sealed class TsDependentSchemasCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "dependentSchemas";
    public int Priority => 55;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("dependentSchemas", out var d) &&
        d.ValueKind == JsonValueKind.Object;

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (context.DetectedDraft < SchemaDraft.Draft201909) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("dependentSchemas", out var depElem) ||
            depElem.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }
        return EmitDependentSchemas(context, depElem);
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];

    internal static string EmitDependentSchemas(TsCodeGenerationContext context, JsonElement depElem)
    {
        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        foreach (var dep in depElem.EnumerateObject())
        {
            var trigger = TsLiteral.String(dep.Name);
            var hash = context.GetSubschemaHash(dep.Value);
            sb.AppendLine($"  if (Object.prototype.hasOwnProperty.call({v}, {trigger})) {{");
            sb.AppendLine($"    if (!{context.GenerateValidateCall(hash)}) return false;");
            sb.AppendLine("  }");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }
}

/// <summary>
/// Generates TypeScript code for the "dependencies" keyword.
/// In Draft 4-7, each dependency value can be an array of required property names
/// or a schema — the combined form later split into dependentRequired/dependentSchemas.
/// Draft 2019-09+ still exercises this through optional compatibility tests.
/// </summary>
public sealed class TsDependenciesCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "dependencies";
    public int Priority => 55;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("dependencies", out var d) &&
        d.ValueKind == JsonValueKind.Object;

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled && context.DetectedDraft >= SchemaDraft.Draft201909)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("dependencies", out var depElem) ||
            depElem.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        foreach (var dep in depElem.EnumerateObject())
        {
            var trigger = TsLiteral.String(dep.Name);
            sb.AppendLine($"  if (Object.prototype.hasOwnProperty.call({v}, {trigger})) {{");
            if (dep.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var required in dep.Value.EnumerateArray())
                {
                    if (required.ValueKind != JsonValueKind.String) continue;
                    sb.AppendLine($"    if (!Object.prototype.hasOwnProperty.call({v}, {TsLiteral.String(required.GetString()!)})) return false;");
                }
            }
            else
            {
                var hash = context.GetSubschemaHash(dep.Value);
                sb.AppendLine($"    if (!{context.GenerateValidateCall(hash)}) return false;");
            }
            sb.AppendLine("  }");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}
