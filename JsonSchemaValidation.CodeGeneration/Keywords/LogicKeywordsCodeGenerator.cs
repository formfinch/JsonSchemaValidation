// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "allOf" keyword.
/// Each branch has isolated annotation state to prevent "cousin" annotations from leaking.
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
        var eval = context.EvaluatedStateVariable;
        var sb = new StringBuilder();

        // If annotation tracking is enabled, isolate each branch's state
        // Each subschema starts with fresh annotations (per JSON Schema spec: unevaluatedProperties
        // only considers annotations from "adjacent keywords" within the same schema object)
        if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
        {
            var branches = allOfElement.EnumerateArray().ToArray();
            sb.AppendLine("// allOf: all subschemas must match (with isolated annotation scopes)");
            sb.AppendLine("{");
            sb.AppendLine($"    var _allOfBase_ = {eval}.Clone();");

            for (int i = 0; i < branches.Length; i++)
            {
                var hash = context.GetSubschemaHash(branches[i]);
                sb.AppendLine($"    // Branch {i} - start with fresh annotations");
                sb.AppendLine($"    {eval}.Reset();");
                sb.AppendLine($"    if (!{context.GenerateValidateCall(hash)}) return false;");
                sb.AppendLine($"    var _allOfBranch{i}_ = {eval}.Clone();");
            }

            // Merge all branches' annotations into the base state
            sb.AppendLine("    // Merge all branches' annotations back to parent");
            sb.AppendLine($"    {eval}.RestoreFrom(_allOfBase_);");
            for (int i = 0; i < branches.Length; i++)
            {
                sb.AppendLine($"    {eval}.MergeFrom(_allOfBranch{i}_);");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        sb.AppendLine("// allOf: all subschemas must match");
        foreach (var subschema in allOfElement.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(subschema);
            sb.AppendLine($"if (!{context.GenerateValidateCall(hash)}) return false;");
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
/// Each branch has isolated annotation state; only successful branches' annotations are merged.
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
        var eval = context.EvaluatedStateVariable;
        var sb = new StringBuilder();

        // If annotation tracking is enabled, isolate each branch's state
        // Each subschema starts with fresh annotations
        if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
        {
            var branches = anyOfElement.EnumerateArray().ToArray();
            sb.AppendLine("// anyOf: at least one subschema must match (with isolated annotation scopes)");
            sb.AppendLine("{");
            sb.AppendLine($"    var _anyOfBase_ = {eval}.Clone();");
            sb.AppendLine("    var _anyOfMatches_ = new List<EvaluatedState>();");

            for (int i = 0; i < branches.Length; i++)
            {
                var hash = context.GetSubschemaHash(branches[i]);
                sb.AppendLine($"    // Branch {i} - start with fresh annotations");
                sb.AppendLine($"    {eval}.Reset();");
                sb.AppendLine($"    if ({context.GenerateValidateCall(hash)})");
                sb.AppendLine($"        _anyOfMatches_.Add({eval}.Clone());");
            }

            sb.AppendLine("    if (_anyOfMatches_.Count == 0) return false;");
            sb.AppendLine("    // Merge successful branches' annotations back to parent");
            sb.AppendLine($"    {eval}.RestoreFrom(_anyOfBase_);");
            sb.AppendLine("    foreach (var _m_ in _anyOfMatches_)");
            sb.AppendLine($"        {eval}.MergeFrom(_m_);");
            sb.AppendLine("}");
            return sb.ToString();
        }

        sb.AppendLine("// anyOf: at least one subschema must match");
        sb.AppendLine("{");
        sb.AppendLine("    var _anyValid_ = false;");

        foreach (var subschema in anyOfElement.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(subschema);
            sb.AppendLine($"    if ({context.GenerateValidateCall(hash)}) _anyValid_ = true;");
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
/// Each branch has isolated annotation state; only the exactly one matching branch's annotations are merged.
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
        var eval = context.EvaluatedStateVariable;
        var sb = new StringBuilder();

        // If annotation tracking is enabled, isolate each branch's state
        // Each subschema starts with fresh annotations
        if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
        {
            var branches = oneOfElement.EnumerateArray().ToArray();
            sb.AppendLine("// oneOf: exactly one subschema must match (with isolated annotation scopes)");
            sb.AppendLine("{");
            sb.AppendLine($"    var _oneOfBase_ = {eval}.Clone();");
            sb.AppendLine("    EvaluatedState? _oneOfMatch_ = null;");
            sb.AppendLine("    var _matchCount_ = 0;");

            for (int i = 0; i < branches.Length; i++)
            {
                var hash = context.GetSubschemaHash(branches[i]);
                sb.AppendLine($"    // Branch {i} - start with fresh annotations");
                sb.AppendLine($"    {eval}.Reset();");
                sb.AppendLine($"    if ({context.GenerateValidateCall(hash)})");
                sb.AppendLine("    {");
                sb.AppendLine("        _matchCount_++;");
                sb.AppendLine("        if (_matchCount_ > 1) return false;");
                sb.AppendLine($"        _oneOfMatch_ = {eval}.Clone();");
                sb.AppendLine("    }");
            }

            sb.AppendLine("    if (_matchCount_ != 1) return false;");
            sb.AppendLine("    // Merge the one matching branch's annotations back to parent");
            sb.AppendLine($"    {eval}.RestoreFrom(_oneOfBase_);");
            sb.AppendLine($"    {eval}.MergeFrom(_oneOfMatch_!);");
            sb.AppendLine("}");
            return sb.ToString();
        }

        sb.AppendLine("// oneOf: exactly one subschema must match");
        sb.AppendLine("{");
        sb.AppendLine("    var _matchCount_ = 0;");

        foreach (var subschema in oneOfElement.EnumerateArray())
        {
            var hash = context.GetSubschemaHash(subschema);
            sb.AppendLine($"    if ({context.GenerateValidateCall(hash)}) _matchCount_++;");
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
/// Annotations from the "not" subschema are never collected (per JSON Schema spec).
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
        var eval = context.EvaluatedStateVariable;
        var hash = context.GetSubschemaHash(notElement);

        // If annotation tracking is enabled, save/restore state to discard annotations from "not"
        // The subschema starts with fresh annotations (which will be discarded)
        if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// not: subschema must NOT match (annotations discarded)");
            sb.AppendLine("{");
            sb.AppendLine($"    {eval}.SaveTo(out var _notSnapshot_);");
            sb.AppendLine($"    {eval}.Reset();");
            sb.AppendLine($"    var _notResult_ = {context.GenerateValidateCall(hash)};");
            sb.AppendLine($"    {eval}.RestoreFrom(_notSnapshot_);");
            sb.AppendLine("    if (_notResult_) return false;");
            sb.AppendLine("}");
            return sb.ToString();
        }

        return $"// not: subschema must NOT match\nif ({context.GenerateValidateCall(hash)}) return false;";
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}

/// <summary>
/// Generates code for the "extends" keyword (Draft 3 only).
/// extends is functionally equivalent to allOf - all extended schemas must validate.
/// </summary>
public sealed class ExtendsCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "extends";
    public int Priority => 30;

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("extends", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        // extends is Draft 3 only - ignore in other drafts
        if (context.DetectedDraft != SchemaDraft.Draft3)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("extends", out var extendsElement))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("// extends (Draft 3): all extended schemas must match");

        if (extendsElement.ValueKind == JsonValueKind.Array)
        {
            // Multiple schemas: all must validate
            foreach (var subschema in extendsElement.EnumerateArray())
            {
                var hash = context.GetSubschemaHash(subschema);
                sb.AppendLine($"if (!{context.GenerateValidateCall(hash)}) return false;");
            }
        }
        else if (extendsElement.ValueKind == JsonValueKind.Object ||
                 extendsElement.ValueKind == JsonValueKind.True ||
                 extendsElement.ValueKind == JsonValueKind.False)
        {
            // Single schema
            var hash = context.GetSubschemaHash(extendsElement);
            sb.AppendLine($"if (!{context.GenerateValidateCall(hash)}) return false;");
        }

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}

/// <summary>
/// Generates code for the "disallow" keyword (Draft 3 only).
/// disallow is the inverse of type - validation fails if instance matches any disallowed type or schema.
/// Can contain type strings or schema objects.
/// </summary>
public sealed class DisallowCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "disallow";
    public int Priority => 95; // Right after type check

    public bool CanGenerate(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("disallow", out _);
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        // disallow is Draft 3 only - ignore in other drafts
        if (context.DetectedDraft != SchemaDraft.Draft3)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("disallow", out var disallowElement))
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();

        if (disallowElement.ValueKind == JsonValueKind.String)
        {
            // Single disallowed type
            var type = disallowElement.GetString()!;
            var check = GetTypeCheck(type, e);
            if (!string.IsNullOrEmpty(check))
            {
                sb.AppendLine($"// disallow (Draft 3): {type}");
                sb.AppendLine($"if ({check}) return false;");
            }
        }
        else if (disallowElement.ValueKind == JsonValueKind.Array)
        {
            // Multiple disallowed types or schemas
            var typeChecks = new List<string>();
            var schemaChecks = new List<string>();

            foreach (var item in disallowElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var type = item.GetString()!;
                    var check = GetTypeCheck(type, e);
                    if (!string.IsNullOrEmpty(check))
                    {
                        typeChecks.Add(check);
                    }
                }
                else if (item.ValueKind == JsonValueKind.Object ||
                         item.ValueKind == JsonValueKind.True ||
                         item.ValueKind == JsonValueKind.False)
                {
                    // Schema object - validation fails if instance matches the schema
                    var hash = context.GetSubschemaHash(item);
                    schemaChecks.Add(context.GenerateValidateCall(hash));
                }
            }

            sb.AppendLine("// disallow (Draft 3): instance must NOT match any of these types/schemas");

            if (typeChecks.Count > 0)
            {
                sb.AppendLine($"if ({string.Join(" || ", typeChecks)}) return false;");
            }

            foreach (var schemaCheck in schemaChecks)
            {
                sb.AppendLine($"if ({schemaCheck}) return false;");
            }
        }
        else if (disallowElement.ValueKind == JsonValueKind.Object ||
                 disallowElement.ValueKind == JsonValueKind.True ||
                 disallowElement.ValueKind == JsonValueKind.False)
        {
            // Single disallowed schema
            var hash = context.GetSubschemaHash(disallowElement);
            sb.AppendLine("// disallow (Draft 3): instance must NOT match this schema");
            sb.AppendLine($"if ({context.GenerateValidateCall(hash)}) return false;");
        }

        return sb.ToString();
    }

    private static string? GetTypeCheck(string type, string e)
    {
        return type switch
        {
            "string" => $"{e}.ValueKind == JsonValueKind.String",
            "number" => $"{e}.ValueKind == JsonValueKind.Number",
            "integer" => $"({e}.ValueKind == JsonValueKind.Number && {e}.TryGetDecimal(out var _disallowInt_) && _disallowInt_ == decimal.Truncate(_disallowInt_))",
            "boolean" => $"({e}.ValueKind == JsonValueKind.True || {e}.ValueKind == JsonValueKind.False)",
            "null" => $"{e}.ValueKind == JsonValueKind.Null",
            "array" => $"{e}.ValueKind == JsonValueKind.Array",
            "object" => $"{e}.ValueKind == JsonValueKind.Object",
            "any" => "true", // "any" matches everything, so disallow "any" rejects everything
            _ => null
        };
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
