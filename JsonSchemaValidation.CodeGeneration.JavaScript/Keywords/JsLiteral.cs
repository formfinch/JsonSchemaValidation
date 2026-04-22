// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Globalization;
using System.Text;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// JavaScript source-literal helpers used by keyword emitters.
/// Centralises escaping so every emitter produces syntactically safe output.
/// </summary>
internal static class JsLiteral
{
    // U+2028 / U+2029 referenced via hex to keep them out of the C# source itself:
    // both code points are line terminators in the C# lexer too, so embedding them
    // inline breaks character-literal parsing.
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;
    private const string LineSeparatorEscape = "\\u2028";
    private const string ParagraphSeparatorEscape = "\\u2029";

    /// <summary>
    /// Emits a JavaScript double-quoted string literal, including delimiters.
    /// Escapes the characters needed for JS source safety.
    /// </summary>
    public static string String(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\f': sb.Append("\\f"); break;
                case '\b': sb.Append("\\b"); break;
                case LineSeparator: sb.Append(LineSeparatorEscape); break;
                case ParagraphSeparator: sb.Append(ParagraphSeparatorEscape); break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    /// <summary>
    /// Emits a JSON element as a JavaScript expression literal. Starts from the raw
    /// JSON text (which is always valid JS grammar) and escapes U+2028/U+2029 inside
    /// string literals. Those code points are legal in JSON strings but were illegal
    /// in JS string literals pre-ES2019, and some tooling still chokes on them.
    /// Literal control characters inside strings are impossible (JSON forbids them).
    /// </summary>
    public static string JsonAsExpression(System.Text.Json.JsonElement element)
    {
        var raw = element.GetRawText();
        if (raw.IndexOf(LineSeparator) < 0 && raw.IndexOf(ParagraphSeparator) < 0)
        {
            return raw;
        }
        return raw
            .Replace(LineSeparator.ToString(), LineSeparatorEscape)
            .Replace(ParagraphSeparator.ToString(), ParagraphSeparatorEscape);
    }

    /// <summary>
    /// Emits a JavaScript RegExp construction expression as <c>new RegExp("pattern")</c>.
    /// Avoids the <c>/pattern/</c> literal form because certain pattern prefixes
    /// (most famously a leading asterisk) tokenize as block-comment or other
    /// invalid syntax and would break module parsing. Invalid ECMAScript regex
    /// grammar surfaces at RegExp construction time rather than as a JS parse
    /// error, which is a friendlier failure mode for consumers.
    /// </summary>
    public static string RegexLiteral(string pattern)
    {
        return $"new RegExp({String(pattern)}, \"u\")";
    }
}
