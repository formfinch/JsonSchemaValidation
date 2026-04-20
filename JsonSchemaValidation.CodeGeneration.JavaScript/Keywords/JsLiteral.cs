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
                case '\u2028': sb.Append("\\u2028"); break; // LINE SEPARATOR — legal in ES2019+ strings, escaped for toolchain/legacy safety
                case '\u2029': sb.Append("\\u2029"); break; // PARAGRAPH SEPARATOR — legal in ES2019+ strings, escaped for toolchain/legacy safety
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
    /// string literals — those code points are legal in JSON strings but were illegal
    /// in JS string literals pre-ES2019, and some tooling still chokes on them.
    /// Literal control characters inside strings are impossible (JSON forbids them).
    /// </summary>
    public static string JsonAsExpression(System.Text.Json.JsonElement element)
    {
        var raw = element.GetRawText();
        if (raw.IndexOf('\u2028') < 0 && raw.IndexOf('\u2029') < 0)
        {
            return raw;
        }
        // Both escape sequences are valid in JSON and JS, so replacing the literal
        // code points with their \uXXXX form preserves semantics in both grammars.
        return raw.Replace("\u2028", "\\u2028").Replace("\u2029", "\\u2029");
    }

    /// <summary>
    /// Emits a JavaScript regex literal like /pattern/ with appropriate escaping of
    /// characters that would terminate the literal or be interpreted as line breaks.
    /// Does not rewrite the regex grammar — the pattern is assumed to be ECMAScript.
    /// </summary>
    public static string RegexLiteral(string pattern)
    {
        var sb = new StringBuilder(pattern.Length + 2);
        sb.Append('/');
        var escapedNext = false;
        foreach (var c in pattern)
        {
            if (escapedNext)
            {
                sb.Append(c);
                escapedNext = false;
                continue;
            }
            switch (c)
            {
                case '\\':
                    sb.Append('\\');
                    escapedNext = true;
                    break;
                case '/':
                    sb.Append("\\/");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\u2028':
                    sb.Append("\\u2028");
                    break;
                case '\u2029':
                    sb.Append("\\u2029");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        sb.Append('/');
        return sb.ToString();
    }
}
