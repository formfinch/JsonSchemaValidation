// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for the "pattern" keyword.
/// Uses the runtime's cached-regex helper so schema patterns are compiled once
/// and then reused across validations. Constructor form remains hidden behind
/// the helper so schema-supplied text never participates in JS tokenisation.
/// Regex timeouts don't exist in JS; pathological patterns are a known limitation
/// inherited from the spec (documented in KNOWN_LIMITATIONS.md).
/// </summary>
public sealed class TsPatternCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "pattern";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("pattern", out var p) &&
               p.ValueKind == JsonValueKind.String;
    }

    public string GenerateCode(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            return string.Empty;
        }

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
        var patternLiteral = TsLiteral.String(pattern);
        return $$"""
            if (typeof {{v}} === "string") {
              const _patternRe = getCachedRegex({{patternLiteral}});
              if (!_patternRe.test({{v}})) return false;
            }
            """;
    }

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context)
    {
        if (!context.ValidationVocabularyEnabled)
        {
            yield break;
        }

        if (context.CurrentSchema.TryGetProperty("pattern", out var patternElement) &&
            patternElement.ValueKind == JsonValueKind.String &&
            !string.IsNullOrEmpty(patternElement.GetString()))
        {
            yield return "getCachedRegex";
        }
    }
}
