// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime;
using Jint;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

/// <summary>
/// Tests drive generated JS through Jint to prove emitted output is executable
/// and produces the expected verdicts. The harness writes the validator and
/// runtime to a temp directory so Jint's default module loader can resolve
/// the emitter's canonical "./jsv-runtime.js" import — the same resolution
/// path a real ESM consumer uses.
/// </summary>
public sealed class JsValidatorHarness
{
    private readonly JsSchemaCodeGenerator _generator = new();

    /// <summary>
    /// Generates JS for the schema and executes it against each data value.
    /// </summary>
    public HarnessResult Evaluate(string schemaJson, IEnumerable<string> dataJsonValues)
    {
        var schemaElement = JsonDocument.Parse(schemaJson).RootElement;
        var genResult = _generator.Generate(schemaElement);
        if (!genResult.Success)
        {
            return new HarnessResult(false, genResult.Error, null, []);
        }

        var validatorSource = genResult.GeneratedCode!;

        var tempDir = Path.Combine(Path.GetTempPath(), "jsv-harness-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, JsRuntime.FileName), JsRuntime.GetSource());
            File.WriteAllText(Path.Combine(tempDir, "validator.js"), validatorSource);

            var engine = new Engine(opts => opts.EnableModules(tempDir));
            var module = engine.Modules.Import("./validator.js");
            var validate = module.Get("validate");

            var verdicts = new List<bool>();
            foreach (var dataJson in dataJsonValues)
            {
                var parsed = engine.Evaluate($"JSON.parse({ToJsStringLiteral(dataJson)})");
                var verdictRaw = engine.Invoke(validate, parsed);
                if (!verdictRaw.IsBoolean())
                {
                    return new HarnessResult(false,
                        $"validate() did not return a boolean (got {verdictRaw.Type}).",
                        validatorSource, verdicts);
                }
                verdicts.Add(verdictRaw.AsBoolean());
            }
            return new HarnessResult(true, null, validatorSource, verdicts);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Probes whether Intl.Segmenter is available in the Jint build.
    /// </summary>
    public static bool IntlSegmenterAvailable()
    {
        var engine = new Engine();
        var result = engine.Evaluate(
            "typeof Intl !== 'undefined' && typeof Intl.Segmenter === 'function'");
        return result.AsBoolean();
    }

    private static string ToJsStringLiteral(string input)
    {
        // Two-level escaping: the result is a JS string literal whose decoded value
        // is JSON text for JSON.parse. U+2028/U+2029 must reach JSON.parse as the
        // JSON escape sequence \u2028 / \u2029 (Jint's JSON.parse rejects literal
        // line terminators). So: replace U+2028 with "\u2028" first, then double
        // all backslashes so the JS string decodes to a literal backslash for JSON.
        return "\"" + input
            .Replace("\u2028", "\\u2028")
            .Replace("\u2029", "\\u2029")
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r") + "\"";
    }

    public sealed record HarnessResult(
        bool Success,
        string? Error,
        string? GeneratedSource,
        IReadOnlyList<bool> Verdicts);
}
