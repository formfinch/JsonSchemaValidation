// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
#if NET10_0
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

[CollectionDefinition(nameof(CodeGeneratorCliCollection), DisableParallelization = true)]
public sealed class CodeGeneratorCliCollection
{
}

[Collection(nameof(CodeGeneratorCliCollection))]
public sealed class CodeGeneratorCliTests
{
    [Fact]
    public async Task Generate_WritesCSharpArtifactThroughRegisteredTarget()
    {
        using var workspace = TemporaryWorkspace.Create();
        var schemaPath = workspace.WriteSchema("""{"type":"string"}""");
        var outputPath = workspace.CreateDirectory("csharp");

        var exitCode = await RunCliAsync(
            "generate",
            "-s", schemaPath,
            "-o", outputPath,
            "-n", "Generated.Tests",
            "-c", "CliValidator");

        Assert.Equal(0, exitCode);
        var validatorPath = Path.Combine(outputPath, "CliValidator.cs");
        Assert.True(File.Exists(validatorPath));
        var source = File.ReadAllText(validatorPath);
        Assert.Contains("namespace Generated.Tests", source);
        Assert.Contains("class CliValidator", source);
    }

    [Fact]
    public async Task GenerateJs_WritesReturnedSourceAndRuntimeArtifacts()
    {
        using var workspace = TemporaryWorkspace.Create();
        var schemaPath = workspace.WriteSchema("""{"type":"string"}""");
        var outputPath = workspace.CreateDirectory("javascript");

        var exitCode = await RunCliAsync(
            "generate-js",
            "-s", schemaPath,
            "-o", outputPath);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(outputPath, "jsv-runtime.js")));
        var validatorPath = Assert.Single(
            Directory.GetFiles(outputPath, "*.js"),
            path => !string.Equals(Path.GetFileName(path), "jsv-runtime.js", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("export function validate(data)", File.ReadAllText(validatorPath));
    }

    [Fact]
    public async Task GenerateTs_NoRuntimeSkipsSupportArtifact()
    {
        using var workspace = TemporaryWorkspace.Create();
        var schemaPath = workspace.WriteSchema("""{"type":"string"}""");
        var outputPath = workspace.CreateDirectory("typescript");

        var exitCode = await RunCliAsync(
            "generate-ts",
            "-s", schemaPath,
            "-o", outputPath,
            "--no-runtime");

        Assert.Equal(0, exitCode);
        Assert.False(File.Exists(Path.Combine(outputPath, "jsv-runtime.ts")));
        var validatorPath = Assert.Single(Directory.GetFiles(outputPath, "*.ts"));
        Assert.Contains("export function validate(data: JsonValue): boolean", File.ReadAllText(validatorPath));
    }

    private static async Task<int> RunCliAsync(params string[] args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            return await Program.RunAsync(args);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private sealed class TemporaryWorkspace : IDisposable
    {
        private readonly string _path;

        private TemporaryWorkspace(string path)
        {
            _path = path;
        }

        public static TemporaryWorkspace Create()
        {
            var path = Path.Combine(Path.GetTempPath(), "jsv-codegen-cli-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TemporaryWorkspace(path);
        }

        public string WriteSchema(string schema)
        {
            var schemaPath = Path.Combine(_path, "schema.json");
            File.WriteAllText(schemaPath, schema);
            return schemaPath;
        }

        public string CreateDirectory(string name)
        {
            var path = Path.Combine(_path, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_path, recursive: true);
            }
            catch
            {
                // Best effort cleanup for Windows file-scanner races.
            }
        }
    }
}
#endif
