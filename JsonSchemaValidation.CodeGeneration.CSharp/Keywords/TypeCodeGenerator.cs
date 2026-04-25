// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for the "type" keyword.
/// </summary>
public sealed class TypeCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "type";
    public int Priority => 100; // Run first - type check is fast fail

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("type", out _);
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("type", out var typeElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return GenerateSingleTypeCheck(typeElement.GetString()!, e);
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            return GenerateMultiTypeCheck(context, typeElement, e);
        }

        return string.Empty;
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CSharpCodeGenerationContext context)
    {
        return [];
    }

    private static string GenerateSingleTypeCheck(string type, string e)
    {
        return type switch
        {
            "string" => $"if ({e}.ValueKind != JsonValueKind.String) return false;",
            "number" => $"if ({e}.ValueKind != JsonValueKind.Number) return false;",
            "integer" => GenerateIntegerCheck(e),
            "boolean" => $"if ({e}.ValueKind != JsonValueKind.True && {e}.ValueKind != JsonValueKind.False) return false;",
            "null" => $"if ({e}.ValueKind != JsonValueKind.Null) return false;",
            "array" => $"if ({e}.ValueKind != JsonValueKind.Array) return false;",
            "object" => $"if ({e}.ValueKind != JsonValueKind.Object) return false;",
            _ => string.Empty
        };
    }

    private static string GenerateIntegerCheck(string e)
    {
        // Match dynamic validator logic: try decimal first, then BigInteger, then double for overflow cases
        return $$"""
if ({{e}}.ValueKind != JsonValueKind.Number) return false;
{
    var _isInt_ = false;
    if ({{e}}.TryGetDecimal(out var _intVal_) && _intVal_ == decimal.Truncate(_intVal_)) _isInt_ = true;
    else if (System.Numerics.BigInteger.TryParse({{e}}.ToString(), System.Globalization.CultureInfo.InvariantCulture, out _)) _isInt_ = true;
    else if ({{e}}.TryGetDouble(out var _dblVal_) && !double.IsInfinity(_dblVal_) && !double.IsNaN(_dblVal_) && Math.Abs(_dblVal_ - Math.Floor(_dblVal_)) < double.Epsilon) _isInt_ = true;
    if (!_isInt_) return false;
}
""";
    }

    private string GenerateMultiTypeCheck(CSharpCodeGenerationContext context, JsonElement typeArray, string e)
    {
        var types = new List<string>();
        var schemaChecks = new List<string>();

        foreach (var item in typeArray.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                types.Add(item.GetString()!);
            }
            else if (context.DetectedDraft == SchemaDraft.Draft3 &&
                     (item.ValueKind == JsonValueKind.Object ||
                      item.ValueKind == JsonValueKind.True ||
                      item.ValueKind == JsonValueKind.False))
            {
                // Draft 3 only: type array can contain schema objects
                var hash = context.GetSubschemaHash(item);
                schemaChecks.Add(context.GenerateValidateCall(hash));
            }
        }

        if (types.Count == 0 && schemaChecks.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("    var _typeValid_ = false;");

        foreach (var type in types)
        {
            var condition = type switch
            {
                "string" => $"{e}.ValueKind == JsonValueKind.String",
                "number" => $"{e}.ValueKind == JsonValueKind.Number",
                "integer" => $"({e}.ValueKind == JsonValueKind.Number && (({e}.TryGetDecimal(out var _iv_) && _iv_ == decimal.Truncate(_iv_)) || System.Numerics.BigInteger.TryParse({e}.ToString(), System.Globalization.CultureInfo.InvariantCulture, out _) || ({e}.TryGetDouble(out var _dv_) && !double.IsInfinity(_dv_) && !double.IsNaN(_dv_) && Math.Abs(_dv_ - Math.Floor(_dv_)) < double.Epsilon)))",
                "boolean" => $"({e}.ValueKind == JsonValueKind.True || {e}.ValueKind == JsonValueKind.False)",
                "null" => $"{e}.ValueKind == JsonValueKind.Null",
                "array" => $"{e}.ValueKind == JsonValueKind.Array",
                "object" => $"{e}.ValueKind == JsonValueKind.Object",
                "any" when context.DetectedDraft == SchemaDraft.Draft3 => "true", // Draft 3 only: "any" matches all types
                _ => null
            };

            if (condition != null)
            {
                sb.AppendLine($"    if ({condition}) _typeValid_ = true;");
            }
        }

        // Draft 3: schema objects in type array - instance must match at least one
        foreach (var schemaCheck in schemaChecks)
        {
            sb.AppendLine($"    if ({schemaCheck}) _typeValid_ = true;");
        }

        sb.AppendLine("    if (!_typeValid_) return false;");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
