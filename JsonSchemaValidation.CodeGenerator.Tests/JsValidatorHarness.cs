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
    private readonly JsSchemaCodeGenerator _generator;

    public JsValidatorHarness(
        bool formatAssertionEnabled = false,
        IReadOnlyDictionary<string, string>? externalSchemaDocuments = null)
    {
        _generator = new JsSchemaCodeGenerator
        {
            FormatAssertionEnabled = formatAssertionEnabled,
            ExternalSchemaDocuments = externalSchemaDocuments
        };
    }

    /// <summary>
    /// Generates JS for the schema and executes it against each data value.
    /// </summary>
    public HarnessResult Evaluate(
        string schemaJson,
        IEnumerable<string> dataJsonValues,
        string? registryExpression = null)
    {
        // JsonElement is backed by JsonDocument's pooled buffers; dispose the
        // document once we've lifted the schema into generator-owned form via Clone.
        using var schemaDoc = JsonDocument.Parse(schemaJson);
        var schemaElement = schemaDoc.RootElement.Clone();
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
            var registry = registryExpression == null ? null : engine.Evaluate(registryExpression);

            var verdicts = new List<bool>();
            foreach (var dataJson in dataJsonValues)
            {
                var parsed = engine.Evaluate($"JSON.parse({JsTestHelpers.ToJsStringLiteral(dataJson)})");
                var verdictRaw = registry == null
                    ? engine.Invoke(validate, parsed)
                    : engine.Invoke(validate, parsed, registry);
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

    public sealed record HarnessResult(
        bool Success,
        string? Error,
        string? GeneratedSource,
        IReadOnlyList<bool> Verdicts);
}
