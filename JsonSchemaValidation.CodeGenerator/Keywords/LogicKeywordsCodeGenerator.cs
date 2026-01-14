using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGenerator.Keywords;

/// <summary>
/// Generates code for the "allOf" keyword.
/// </summary>
public sealed class AllOfCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "allOf";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("allOf", out var arr) &&
               arr.ValueKind == JsonValueKind.Array;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("allOf", out var allOfElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();
        sb.AppendLine("// allOf: all subschemas must match");

        foreach (var subschema in allOfElement.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(subschema);
            sb.AppendLine($"if (!Validate_{hash}({e})) return false;");
        }

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}

/// <summary>
/// Generates code for the "anyOf" keyword.
/// </summary>
public sealed class AnyOfCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "anyOf";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("anyOf", out var arr) &&
               arr.ValueKind == JsonValueKind.Array;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("anyOf", out var anyOfElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();
        sb.AppendLine("// anyOf: at least one subschema must match");
        sb.AppendLine("{");
        sb.AppendLine("    var _anyValid_ = false;");

        foreach (var subschema in anyOfElement.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(subschema);
            sb.AppendLine($"    if (Validate_{hash}({e})) _anyValid_ = true;");
        }

        sb.AppendLine("    if (!_anyValid_) return false;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}

/// <summary>
/// Generates code for the "oneOf" keyword.
/// </summary>
public sealed class OneOfCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "oneOf";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("oneOf", out var arr) &&
               arr.ValueKind == JsonValueKind.Array;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("oneOf", out var oneOfElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();
        sb.AppendLine("// oneOf: exactly one subschema must match");
        sb.AppendLine("{");
        sb.AppendLine("    var _matchCount_ = 0;");

        foreach (var subschema in oneOfElement.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(subschema);
            sb.AppendLine($"    if (Validate_{hash}({e})) _matchCount_++;");
            sb.AppendLine("    if (_matchCount_ > 1) return false;");
        }

        sb.AppendLine("    if (_matchCount_ != 1) return false;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}

/// <summary>
/// Generates code for the "not" keyword.
/// </summary>
public sealed class NotCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "not";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("not", out var notSchema) &&
               (notSchema.ValueKind == JsonValueKind.Object ||
                notSchema.ValueKind == JsonValueKind.True ||
                notSchema.ValueKind == JsonValueKind.False);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("not", out var notElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var hash = context.GetSubschemaHash(notElement);

        return $"// not: subschema must NOT match\nif (Validate_{hash}({e})) return false;";
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
