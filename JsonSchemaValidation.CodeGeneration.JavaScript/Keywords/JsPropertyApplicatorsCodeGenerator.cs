// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for the "patternProperties" keyword.
/// </summary>
public sealed class JsPatternPropertiesCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "patternProperties";
    public int Priority => 45;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("patternProperties", out var p) &&
        p.ValueKind == JsonValueKind.Object;

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("patternProperties", out var patternsElem) ||
            patternsElem.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        // Empty patternProperties {}: no pattern checks to emit.
        var patternEnumerator = patternsElem.EnumerateObject();
        if (!patternEnumerator.MoveNext())
        {
            return string.Empty;
        }

        var v = context.ElementExpr;
        // Hoist each pattern's RegExp outside the key loop. Without this we'd
        // allocate a new RegExp per property name on every validate call;
        // hoisting gives one RegExp per validate invocation instead.
        var hoists = new StringBuilder();
        var checks = new StringBuilder();
        var idx = 0;
        do
        {
            var pattern = patternEnumerator.Current;
            var regex = JsLiteral.RegexLiteral(pattern.Name);
            var regexVar = $"_ppRe{idx}";
            var hash = context.GetSubschemaHash(pattern.Value);
            hoists.AppendLine($"  const {regexVar} = {regex};");
            checks.AppendLine($"    if ({regexVar}.test(_k)) {{");
            checks.AppendLine($"      if (!{context.GenerateValidateCallForProperty(hash, $"{v}[_k]", "_k")}) return false;");
            if (context.RequiresPropertyAnnotations)
            {
                checks.AppendLine($"      {context.EvaluatedStateExpr}.markPropertyEvaluated({context.LocationExpr}, _k);");
            }
            checks.AppendLine("    }");
            idx++;
        }
        while (patternEnumerator.MoveNext());

        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        sb.Append(hoists);
        sb.AppendLine($"  for (const _k of Object.keys({v})) {{");
        sb.Append(checks);
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}

/// <summary>
/// Generates JavaScript code for the "additionalProperties" keyword.
/// Handles the three forms: false (reject unlisted), schema (validate unlisted), true (no-op).
/// </summary>
public sealed class JsAdditionalPropertiesCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "additionalProperties";
    public int Priority => 20;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("additionalProperties", out _);

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("additionalProperties", out var addProps))
        {
            return string.Empty;
        }
        if (addProps.ValueKind == JsonValueKind.True)
        {
            if (!context.RequiresPropertyAnnotations)
            {
                return string.Empty;
            }
        }

        var definedNames = CollectDefinedNames(context.CurrentSchema);
        var patternRegexes = CollectPatternRegexes(context.CurrentSchema);

        var v = context.ElementExpr;
        // Hoist pattern regexes outside the key loop (same reason as
        // patternProperties — avoid one RegExp allocation per property).
        var hoists = new StringBuilder();
        var regexVars = new List<string>();
        for (var i = 0; i < patternRegexes.Count; i++)
        {
            var rv = $"_apRe{i}";
            hoists.AppendLine($"  const {rv} = {patternRegexes[i]};");
            regexVars.Add(rv);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        sb.Append(hoists);
        sb.AppendLine($"  for (const _k of Object.keys({v})) {{");
        if (definedNames.Count > 0)
        {
            var conds = definedNames.Select(n => $"_k === {JsLiteral.String(n)}");
            sb.AppendLine($"    if ({string.Join(" || ", conds)}) continue;");
        }
        if (regexVars.Count > 0)
        {
            var conds = regexVars.Select(r => $"{r}.test(_k)");
            sb.AppendLine($"    if ({string.Join(" || ", conds)}) continue;");
        }
        if (addProps.ValueKind == JsonValueKind.False)
        {
            sb.AppendLine("    return false;");
        }
        else if (addProps.ValueKind == JsonValueKind.True)
        {
            sb.AppendLine($"    {context.EvaluatedStateExpr}.markPropertyEvaluated({context.LocationExpr}, _k);");
        }
        else if (addProps.ValueKind == JsonValueKind.Object)
        {
            var hash = context.GetSubschemaHash(addProps);
            sb.AppendLine($"    if (!{context.GenerateValidateCallForProperty(hash, $"{v}[_k]", "_k")}) return false;");
            if (context.RequiresPropertyAnnotations)
            {
                sb.AppendLine($"    {context.EvaluatedStateExpr}.markPropertyEvaluated({context.LocationExpr}, _k);");
            }
        }
        else
        {
            // Invalid schema: additionalProperties must be object/boolean per
            // spec. SubschemaExtractor doesn't collect non-schema values, so
            // emitting a validate_<hash> call would reference an undefined
            // function at runtime. Fail fast with an actionable message,
            // matching how the gate treats other invalid-schema shapes.
            throw new InvalidOperationException(
                "Schema's \"additionalProperties\" value must be an object or boolean; got " +
                $"{addProps.ValueKind}.");
        }
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];

    private static List<string> CollectDefinedNames(JsonElement schema)
    {
        var names = new List<string>();
        if (schema.TryGetProperty("properties", out var props) &&
            props.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in props.EnumerateObject())
            {
                names.Add(p.Name);
            }
        }
        return names;
    }

    private static List<string> CollectPatternRegexes(JsonElement schema)
    {
        var regexes = new List<string>();
        if (schema.TryGetProperty("patternProperties", out var patterns) &&
            patterns.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in patterns.EnumerateObject())
            {
                regexes.Add(JsLiteral.RegexLiteral(p.Name));
            }
        }
        return regexes;
    }
}

/// <summary>
/// Generates JavaScript code for the "propertyNames" keyword (Draft 6+).
/// </summary>
public sealed class JsPropertyNamesCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "propertyNames";
    public int Priority => 60;

    public bool CanGenerate(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object &&
        schema.TryGetProperty("propertyNames", out _);

    public string GenerateCode(JsCodeGenerationContext context)
    {
        // propertyNames was introduced in Draft 6; Draft 4 must ignore it.
        if (context.DetectedDraft < SchemaDraft.Draft6) return string.Empty;
        if (!context.CurrentSchema.TryGetProperty("propertyNames", out var pn))
        {
            return string.Empty;
        }
        if (pn.ValueKind != JsonValueKind.Object &&
            pn.ValueKind != JsonValueKind.True &&
            pn.ValueKind != JsonValueKind.False)
        {
            // Invalid schema: propertyNames must be a schema (object or boolean).
            // Non-schema values aren't extracted by SubschemaExtractor, so the
            // emitted validate_<hash> call would be undefined at runtime.
            throw new InvalidOperationException(
                "Schema's \"propertyNames\" value must be an object or boolean; got " +
                $"{pn.ValueKind}.");
        }
        var hash = context.GetSubschemaHash(pn);
        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");
        sb.AppendLine($"  for (const _k of Object.keys({v})) {{");
        sb.AppendLine($"    if (!{context.GenerateValidateCallForExpr(hash, "_k")}) return false;");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}
