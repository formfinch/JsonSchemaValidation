// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for the "properties" keyword.
/// </summary>
public sealed class JsPropertiesCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "properties";
    public int Priority => 50;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("properties", out var props) &&
               props.ValueKind == JsonValueKind.Object;
    }

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var v = context.ElementExpr;
        var sb = new StringBuilder();
        sb.AppendLine($"if (typeof {v} === \"object\" && {v} !== null && !Array.isArray({v})) {{");

        foreach (var prop in properties.EnumerateObject())
        {
            var nameLiteral = JsLiteral.String(prop.Name);
            var call = context.GenerateValidateCallForProperty(prop.Value, $"{v}[{nameLiteral}]", nameLiteral);
            sb.AppendLine($"  if (Object.prototype.hasOwnProperty.call({v}, {nameLiteral})) {{");
            sb.AppendLine($"    if (!{call}) return false;");
            if (context.RequiresPropertyAnnotations)
            {
                sb.AppendLine($"    {context.EvaluatedStateExpr}.markPropertyEvaluated({context.LocationExpr}, {nameLiteral});");
            }
            sb.AppendLine("  }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}
