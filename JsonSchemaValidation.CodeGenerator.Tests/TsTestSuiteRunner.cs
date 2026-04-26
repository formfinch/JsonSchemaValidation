// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;
using Jint;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

/// <summary>
/// Precompiles the Draft 2020-12 JSON-Schema-Test-Suite through the TypeScript
/// generator and tsc. The per-case tests execute the compiled JS output, so this
/// proves the TS-first pipeline rather than the direct JS emitter.
/// </summary>
public sealed class TsDraft202012SuiteFixture : IDisposable
{
    private const int TscBatchSize = 20;

    private readonly string _moduleRoot;
    private readonly string _sourceRoot;
    private readonly Dictionary<string, string> _externalSchemaDocuments = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _compiledModulesByCaseKey = new(StringComparer.Ordinal);

    public TsDraft202012SuiteFixture()
    {
        if (!TypeScriptCompiler.IsAvailable())
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "TypeScript compiler 'tsc' is required for the TS JSON-Schema-Test-Suite runner.");
        }

        _moduleRoot = Path.Combine(Path.GetTempPath(), "jsv-ts-suite-fixture-" + Guid.NewGuid().ToString("N"));
        _sourceRoot = Path.Combine(_moduleRoot, "ts-src");
        Directory.CreateDirectory(_sourceRoot);
        File.WriteAllText(Path.Combine(_sourceRoot, TsRuntime.FileName), TsRuntime.GetSource());

        var sourcePaths = new List<string>();
        var (remoteSourcePaths, remoteRegistrations) = GenerateRemoteSources();
        sourcePaths.AddRange(remoteSourcePaths);
        WriteRegistryModule(remoteRegistrations);
        sourcePaths.AddRange(GenerateSuiteValidatorSources());
        CompileSources(sourcePaths);
    }

    public string ModuleRoot => _moduleRoot;

    public void Dispose()
    {
        try { Directory.Delete(_moduleRoot, recursive: true); } catch { /* best effort */ }
    }

    public string GetCompiledModuleFile(JsTestSuiteRunner.TestCase testCase)
    {
        return _compiledModulesByCaseKey[GetCaseKey(testCase)];
    }

    private (List<string> SourcePaths, List<RemoteRegistration> Registrations) GenerateRemoteSources()
    {
        var pendingSchemas = new List<(Uri SchemaUri, string Content)>();
        var suitePath = JsonSchemaTestSuiteHelpers.FindTestSuitePath();
        if (suitePath == null)
        {
            return ([], []);
        }

        var remotesPath = Path.Combine(suitePath, "remotes");
        JsonSchemaTestSuiteHelpers.CollectRemotesFromPath(
            pendingSchemas,
            Path.Combine(remotesPath, "draft2020-12"),
            "http://localhost:1234/draft2020-12/");
        JsonSchemaTestSuiteHelpers.CollectRemotesFromPath(
            pendingSchemas,
            Path.Combine(remotesPath, "draft2019-09"),
            "http://localhost:1234/draft2019-09/");
        JsonSchemaTestSuiteHelpers.CollectRemotesFromPath(
            pendingSchemas,
            remotesPath,
            "http://localhost:1234/",
            topLevelOnly: true);
        JsonSchemaTestSuiteHelpers.CollectBundledDraft202012Schemas(pendingSchemas);

        foreach (var (schemaUri, content) in pendingSchemas)
        {
            _externalSchemaDocuments[schemaUri.AbsoluteUri] = content;
        }

        var generator = new TsSchemaCodeGenerator
        {
            AlwaysTrackAnnotations = true,
            ExternalSchemaDocuments = _externalSchemaDocuments
        };
        var sourcePaths = new List<string>();
        var registrations = new List<RemoteRegistration>();
        var seenUris = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;

        foreach (var (schemaUri, content) in pendingSchemas)
        {
            if (!seenUris.Add(schemaUri.AbsoluteUri))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(content);
                var result = generator.Generate(doc.RootElement.Clone(), sourcePath: $"remote_{index}.json");
                if (!result.Success)
                {
                    continue;
                }

                var moduleName = $"remote_{index}.ts";
                var sourcePath = Path.Combine(_sourceRoot, moduleName);
                File.WriteAllText(sourcePath, result.GeneratedCode!);
                sourcePaths.Add(sourcePath);
                registrations.Add(new RemoteRegistration(schemaUri.AbsoluteUri, Path.ChangeExtension(moduleName, ".js")));
                index++;
            }
            catch
            {
                // Remote preload is best effort. Cases that require unsupported
                // remotes fail visibly when the registry cannot resolve them.
            }
        }

        return (sourcePaths, registrations);
    }

    private List<string> GenerateSuiteValidatorSources()
    {
        var cases = EnumerateCasesForPrecompile()
            .Where(tc => !tc.IsMissingSuiteSentinel)
            .GroupBy(GetCaseKey, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        var sourcePaths = new List<string>(cases.Count);
        var index = 0;
        foreach (var testCase in cases)
        {
            var generator = new TsSchemaCodeGenerator
            {
                DefaultDraft = testCase.Draft,
                FormatAssertionEnabled = testCase.FormatAssertionEnabled,
                ExternalSchemaDocuments = _externalSchemaDocuments
            };

            using var schemaDoc = JsonDocument.Parse(testCase.SchemaJson);
            var result = generator.Generate(schemaDoc.RootElement.Clone(), sourcePath: $"validator_{index}.json");
            if (!result.Success)
            {
                throw new InvalidOperationException($"TS codegen failed for {testCase}: {result.Error}");
            }

            var moduleName = $"validator_{index}.ts";
            var sourcePath = Path.Combine(_sourceRoot, moduleName);
            File.WriteAllText(sourcePath, result.GeneratedCode!);
            sourcePaths.Add(sourcePath);
            _compiledModulesByCaseKey[GetCaseKey(testCase)] = Path.ChangeExtension(moduleName, ".js");
            index++;
        }

        return sourcePaths;
    }

    private void CompileSources(IReadOnlyList<string> sourcePaths)
    {
        var runtimePath = Path.Combine(_sourceRoot, TsRuntime.FileName);
        foreach (var batch in sourcePaths.Chunk(TscBatchSize))
        {
            var compileResult = TypeScriptCompiler.Compile(
                [runtimePath, .. batch],
                _moduleRoot,
                ecmaScriptTarget: "ES2020",
                timeoutMilliseconds: 240_000);
            if (!compileResult.Success)
            {
                throw new InvalidOperationException(
                    $"TypeScript suite compilation failed: {compileResult.Error}\n{compileResult.StandardError}\n{compileResult.StandardOutput}");
            }
        }
    }

    private void WriteRegistryModule(IReadOnlyList<RemoteRegistration> remoteRegistrations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Auto-generated by TsDraft202012SuiteFixture.");
        sb.AppendLine("import { Registry } from \"./jsv-runtime.js\";");

        for (var i = 0; i < remoteRegistrations.Count; i++)
        {
            sb.AppendLine($"import remote_{i}, {{ fragmentValidators as remoteFragments_{i} }} from \"./{remoteRegistrations[i].ModuleName}\";");
        }

        sb.AppendLine();
        sb.AppendLine("const registry = new Registry();");
        for (var i = 0; i < remoteRegistrations.Count; i++)
        {
            sb.AppendLine($"registry.registerForUri({JsonSerializer.Serialize(remoteRegistrations[i].Uri)}, remote_{i});");
            sb.AppendLine($"for (const [uri, validator] of Object.entries(remoteFragments_{i})) registry.registerForUri(uri, validator);");
        }
        sb.AppendLine();
        sb.AppendLine("export default registry;");

        File.WriteAllText(Path.Combine(_moduleRoot, "registry.js"), sb.ToString());
    }

    private static IEnumerable<JsTestSuiteRunner.TestCase> EnumerateCasesForPrecompile()
    {
        return JsTestSuiteRunner.Draft202012Cases()
            .Concat(JsTestSuiteRunner.Draft202012FormatAssertionCases())
            .Select(row => (JsTestSuiteRunner.TestCase)row[0]);
    }

    private static string GetCaseKey(JsTestSuiteRunner.TestCase testCase)
    {
        var input = $"{testCase.Draft}|{testCase.FormatAssertionEnabled}|{testCase.SchemaJson}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private sealed record RemoteRegistration(string Uri, string ModuleName);
}

public class TsTestSuiteRunner : IClassFixture<TsDraft202012SuiteFixture>
{
    private readonly TsDraft202012SuiteFixture _fixture;

    public TsTestSuiteRunner(TsDraft202012SuiteFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [MemberData(nameof(Draft202012Cases))]
    public void Draft202012(JsTestSuiteRunner.TestCase tc) => RunCase(tc);

    [Theory]
    [MemberData(nameof(Draft202012FormatAssertionCases))]
    public void Draft202012FormatAssertion(JsTestSuiteRunner.TestCase tc) => RunCase(tc);

    public static IEnumerable<object[]> Draft202012Cases() => JsTestSuiteRunner.Draft202012Cases();

    public static IEnumerable<object[]> Draft202012FormatAssertionCases() => JsTestSuiteRunner.Draft202012FormatAssertionCases();

    private void RunCase(JsTestSuiteRunner.TestCase tc)
    {
        if (tc.IsMissingSuiteSentinel)
        {
            Assert.Fail(
                "JSON-Schema-Test-Suite submodule is not available on this machine. " +
                "Run 'git submodule update --init' before running the TS suite runner.");
        }

        var moduleFile = _fixture.GetCompiledModuleFile(tc);
        var engine = new Engine(opts => opts.EnableModules(_fixture.ModuleRoot));
        var registry = engine.Modules.Import("./registry.js").Get("default");
        var module = engine.Modules.Import("./" + moduleFile);
        var validate = module.Get("validate");

        var parsed = engine.Evaluate($"JSON.parse({JsTestHelpers.ToJsStringLiteral(tc.DataJson)})");
        var verdictRaw = engine.Invoke(validate, parsed, registry);
        Assert.True(verdictRaw.IsBoolean(), $"Non-boolean verdict for {tc}");
        var actual = verdictRaw.AsBoolean();
        Assert.True(actual == tc.Expected,
            $"{tc}\nExpected: {tc.Expected}, Got: {actual}\nSchema: {tc.SchemaJson}\nData: {tc.DataJson}");
    }
}
