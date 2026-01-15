using System.Text;
using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "if/then/else" keywords.
/// </summary>
public sealed class IfThenElseCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "if/then/else";
    public int Priority => 25;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("if", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("if", out var ifElement))
        {
            return string.Empty;
        }

        var hasThen = context.CurrentSchema.TryGetProperty("then", out var thenElement);
        var hasElse = context.CurrentSchema.TryGetProperty("else", out var elseElement);

        if (!hasThen && !hasElse)
        {
            return string.Empty; // if alone has no validation effect
        }

        var e = context.ElementVariable;
        var ifHash = context.GetSubschemaHash(ifElement);
        var sb = new StringBuilder();

        sb.AppendLine("// if/then/else");
        sb.AppendLine($"if (Validate_{ifHash}({e}))");
        sb.AppendLine("{");

        if (hasThen)
        {
            var thenHash = context.GetSubschemaHash(thenElement);
            sb.AppendLine($"    if (!Validate_{thenHash}({e})) return false;");
        }

        sb.AppendLine("}");

        if (hasElse)
        {
            sb.AppendLine("else");
            sb.AppendLine("{");
            var elseHash = context.GetSubschemaHash(elseElement);
            sb.AppendLine($"    if (!Validate_{elseHash}({e})) return false;");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
