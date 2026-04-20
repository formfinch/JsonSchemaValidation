// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for the "pattern" keyword.
/// Emits a native RegExp literal — ECMAScript regex, matching JSON Schema intent.
/// Regex timeouts don't exist in JS; pathological patterns are a known limitation
/// inherited from the spec (documented in KNOWN_LIMITATIONS.md).
/// </summary>
public sealed class JsPatternCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "pattern";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("pattern", out var p) &&
               p.ValueKind == JsonValueKind.String;
    }

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("pattern", out var patternElement))
        {
            return string.Empty;
        }

        var pattern = patternElement.GetString();
        if (string.IsNullOrEmpty(pattern))
        {
            // Dynamic validator rejects empty "pattern" as an invalid schema; surface
            // the same intent here rather than silently emitting no regex check.
            throw new InvalidOperationException(
                "Schema has an empty \"pattern\" string, which is invalid — pattern must be " +
                "a non-empty ECMA-262 regular expression.");
        }

        var v = context.ElementExpr;
        var literal = JsLiteral.RegexLiteral(pattern);
        return $"if (typeof {v} === \"string\" && !{literal}.test({v})) return false;";
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}
