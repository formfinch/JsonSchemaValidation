// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for the "contentEncoding" and "contentMediaType" keywords.
/// These keywords are only supported in Draft 7 for content assertion validation.
/// contentEncoding: validates base64 encoding
/// contentMediaType: validates JSON content (application/json)
/// </summary>
public sealed class ContentCodeGenerator : IKeywordCodeGenerator
{
    public string Keyword => "content";
    public int Priority => 35; // Run after format validation

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // Check for contentEncoding or contentMediaType
        return schema.TryGetProperty("contentEncoding", out var encodingElement) &&
               encodingElement.ValueKind == JsonValueKind.String ||
               schema.TryGetProperty("contentMediaType", out var mediaTypeElement) &&
               mediaTypeElement.ValueKind == JsonValueKind.String;
    }

    public string GenerateCode(CodeGenerationContext context)
    {
        // Only Draft 7 performs content validation (assertion mode)
        // Later drafts treat content keywords as annotations only
        if (context.DetectedDraft != SchemaDraft.Draft7)
        {
            return string.Empty;
        }

        var hasEncoding = context.CurrentSchema.TryGetProperty("contentEncoding", out var encodingElement) &&
                          encodingElement.ValueKind == JsonValueKind.String;
        var hasMediaType = context.CurrentSchema.TryGetProperty("contentMediaType", out var mediaTypeElement) &&
                           mediaTypeElement.ValueKind == JsonValueKind.String;

        if (!hasEncoding && !hasMediaType)
        {
            return string.Empty;
        }

        var encoding = hasEncoding ? encodingElement.GetString() : null;
        var mediaType = hasMediaType ? mediaTypeElement.GetString() : null;

        var isBase64 = string.Equals(encoding, "base64", StringComparison.OrdinalIgnoreCase);
        var isJson = string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase);

        // Only generate code for supported encodings/media types
        if (!isBase64 && !isJson)
        {
            return string.Empty;
        }

        var e = context.ElementVariable;
        var sb = new StringBuilder();

        sb.AppendLine($"if ({e}.ValueKind == JsonValueKind.String)");
        sb.AppendLine("{");
        sb.AppendLine($"    var _contentValue_ = {e}.GetString();");
        sb.AppendLine("    if (_contentValue_ != null)");
        sb.AppendLine("    {");

        if (isBase64)
        {
            // Validate base64 encoding
            sb.AppendLine("        string? _decodedContent_ = null;");
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine("            // Check for valid base64 characters");
            sb.AppendLine("            foreach (char c in _contentValue_)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (!char.IsLetterOrDigit(c) && c != '+' && c != '/' && c != '=' && !char.IsWhiteSpace(c))");
            sb.AppendLine("                    return false;");
            sb.AppendLine("            }");
            sb.AppendLine("            var _bytes_ = Convert.FromBase64String(_contentValue_);");
            sb.AppendLine("            _decodedContent_ = System.Text.Encoding.UTF8.GetString(_bytes_);");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (FormatException)");
            sb.AppendLine("        {");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");

            if (isJson)
            {
                // Also validate JSON after decoding
                sb.AppendLine("        try");
                sb.AppendLine("        {");
                sb.AppendLine("            using var _doc_ = JsonDocument.Parse(_decodedContent_!);");
                sb.AppendLine("        }");
                sb.AppendLine("        catch (JsonException)");
                sb.AppendLine("        {");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
            }
        }
        else if (isJson)
        {
            // Validate JSON directly
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine("            using var _doc_ = JsonDocument.Parse(_contentValue_);");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (JsonException)");
            sb.AppendLine("        {");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        // No static fields needed - uses built-in Convert.FromBase64String and JsonDocument.Parse
        yield break;
    }
}
