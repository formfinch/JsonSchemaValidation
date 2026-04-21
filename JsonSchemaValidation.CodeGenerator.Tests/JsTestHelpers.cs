// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

/// <summary>
/// Shared helpers for JS-target tests. Keeps escape/quoting rules in one place
/// so harness and suite runner can't drift.
/// </summary>
internal static class JsTestHelpers
{
    // U+2028 / U+2029 kept out of source via hex casts; both are line
    // terminators in the C# lexer and would break character-literal parsing.
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;

    /// <summary>
    /// Wraps <paramref name="input"/> as a JS string literal whose decoded
    /// value is the same text, suitable for feeding to <c>JSON.parse(...)</c>
    /// inside Jint. Two-level escaping: U+2028/U+2029 first so the JSON layer
    /// preserves them as escape sequences (Jint's JSON.parse rejects literal
    /// line terminators inside JSON strings), then backslashes/quotes/newlines
    /// so the JS string literal itself is well-formed.
    /// </summary>
    public static string ToJsStringLiteral(string input)
    {
        return "\"" + input
            .Replace(LineSeparator.ToString(), "\\u2028")
            .Replace(ParagraphSeparator.ToString(), "\\u2029")
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r") + "\"";
    }
}
