// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.TypeScript;

/// <summary>
/// Thin wrapper around the TypeScript compiler used by the TS-first JS pipeline.
/// </summary>
public static partial class TypeScriptCompiler
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private const int MinimumMajorVersion = 5;
    private const int MinimumMinorVersion = 0;

    /// <summary>
    /// Returns true when a TypeScript compiler executable can be found.
    /// </summary>
    public static bool IsAvailable(string tscExecutable = "tsc")
    {
        try
        {
            var result = RunTsc(
                tscExecutable,
                ["--version"],
                workingDirectory: null,
                timeoutMilliseconds: 10_000);
            return result.ExitCode == 0 && IsSupportedVersionOutput(result.StandardOutput);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Compiles generated TypeScript modules to ESM JavaScript with an explicit tsc target.
    /// </summary>
    public static TypeScriptCompilationResult Compile(
        IReadOnlyList<string> sourcePaths,
        string outputDirectory,
        string ecmaScriptTarget,
        string tscExecutable = "tsc",
        int timeoutMilliseconds = 60_000)
    {
        if (sourcePaths.Count == 0)
        {
            return TypeScriptCompilationResult.Failed("At least one TypeScript source path is required.");
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return TypeScriptCompilationResult.Failed("An output directory is required.");
        }

        if (string.IsNullOrWhiteSpace(ecmaScriptTarget) ||
            !TscTargetRegex().IsMatch(ecmaScriptTarget))
        {
            return TypeScriptCompilationResult.Failed(
                $"Invalid ECMAScript target '{ecmaScriptTarget}'. Pass a tsc-compatible target such as ES2020 or ESNext.");
        }

        Directory.CreateDirectory(outputDirectory);

        var arguments = new List<string>
        {
            "--target", ecmaScriptTarget,
            "--module", "ES2020",
            "--moduleResolution", "Bundler",
            "--lib", "ES2022,DOM",
            "--outDir", outputDirectory,
            "--noImplicitAny", "false",
            "--strict", "false",
            "--skipLibCheck", "true",
            "--declaration", "false",
            "--sourceMap", "false",
            "--ignoreDeprecations", "6.0"
        };
        arguments.AddRange(sourcePaths);

        try
        {
            var versionResult = RunTsc(
                tscExecutable,
                ["--version"],
                workingDirectory: null,
                timeoutMilliseconds: Math.Min(timeoutMilliseconds, 10_000));
            if (versionResult.ExitCode != 0 || !IsSupportedVersionOutput(versionResult.StandardOutput))
            {
                var detectedVersion = string.IsNullOrWhiteSpace(versionResult.StandardOutput)
                    ? "<unknown>"
                    : versionResult.StandardOutput.Trim();
                return TypeScriptCompilationResult.Failed(
                    $"TypeScript compiler {MinimumMajorVersion}.{MinimumMinorVersion}+ is required for the TS codegen pipeline " +
                    $"because it emits with --moduleResolution Bundler. Detected: {detectedVersion}",
                    versionResult.StandardOutput,
                    versionResult.StandardError);
            }

            var result = RunTsc(tscExecutable, arguments, workingDirectory: null, timeoutMilliseconds);
            if (result.ExitCode == 0)
            {
                return TypeScriptCompilationResult.Succeeded(result.StandardOutput, result.StandardError);
            }

            return TypeScriptCompilationResult.Failed(
                $"tsc failed with exit code {result.ExitCode}.",
                result.StandardOutput,
                result.StandardError);
        }
        catch (Exception ex)
        {
            return TypeScriptCompilationResult.Failed($"Failed to invoke tsc: {ex.Message}");
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_.-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex TscTargetRegex();

    [GeneratedRegex(@"Version\s+(?<major>\d+)\.(?<minor>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex TscVersionRegex();

    private static bool IsSupportedVersionOutput(string output)
    {
        var match = TscVersionRegex().Match(output);
        if (!match.Success ||
            !int.TryParse(match.Groups["major"].Value, out var major) ||
            !int.TryParse(match.Groups["minor"].Value, out var minor))
        {
            return false;
        }

        return major > MinimumMajorVersion ||
               (major == MinimumMajorVersion && minor >= MinimumMinorVersion);
    }

    private static ProcessResult RunTsc(
        string tscExecutable,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        int timeoutMilliseconds)
    {
        var startInfo = CreateStartInfo(tscExecutable, arguments);
        if (!string.IsNullOrEmpty(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        var startedAt = Stopwatch.GetTimestamp();
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start TypeScript compiler process.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(timeoutMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"TypeScript compiler did not finish within {timeoutMilliseconds} ms.");
        }

        var elapsedMilliseconds = (int)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        var remainingMilliseconds = Math.Max(1, timeoutMilliseconds - elapsedMilliseconds);
        if (!Task.WaitAll([standardOutputTask, standardErrorTask], remainingMilliseconds))
        {
            throw new TimeoutException($"TypeScript compiler output did not finish within {timeoutMilliseconds} ms.");
        }

        return new ProcessResult(
            process.ExitCode,
            standardOutputTask.Result,
            standardErrorTask.Result);
    }

    private static ProcessStartInfo CreateStartInfo(string tscExecutable, IReadOnlyList<string> arguments)
    {
        if (OperatingSystem.IsWindows())
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Utf8NoBom,
                StandardErrorEncoding = Utf8NoBom
            };
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/s");
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(BuildWindowsCommand(tscExecutable, arguments));
            return startInfo;
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = tscExecutable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom
        };
        foreach (var arg in arguments)
        {
            processStartInfo.ArgumentList.Add(arg);
        }
        return processStartInfo;
    }

    private static string BuildWindowsCommand(string tscExecutable, IReadOnlyList<string> arguments)
    {
        var parts = new List<string> { QuoteWindowsArgument(tscExecutable) };
        parts.AddRange(arguments.Select(QuoteWindowsArgument));
        return string.Join(" ", parts);
    }

    private static string QuoteWindowsArgument(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        if (!value.Any(c => char.IsWhiteSpace(c) || c == '"' || c == '&' || c == '|' || c == '<' || c == '>' || c == '^'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
