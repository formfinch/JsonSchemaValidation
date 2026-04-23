// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Infrastructure;

/// <summary>
/// Persistent Node.js host for JS validator benchmarks.
/// Uses the shared benchmark adapter so Ajv and generated validators are measured
/// through the same transport.
/// </summary>
public sealed class NodeBenchmarkHost : IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;
    private bool _disposed;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public NodeBenchmarkHost()
    {
        var adapterPath = LocateAdapterHostPath();
        var workingDirectory = Path.GetDirectoryName(adapterPath)
            ?? throw new InvalidOperationException($"Could not determine adapter host directory for {adapterPath}.");

        var startInfo = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $"\"{adapterPath}\"",
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = Utf8NoBom,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom
        };

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Node.js benchmark host. Ensure Node.js is installed and on PATH.");
        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;

        var ready = _stdout.ReadLine();
        if (!string.Equals(ready, "ready", StringComparison.Ordinal))
        {
            var stderr = _process.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"Node.js benchmark host failed to initialize. Ready line: '{ready ?? "<null>"}'. STDERR: {stderr}");
        }
    }

    public void PrepareAjv(string schemaJson)
    {
        Send(new
        {
            cmd = "prepare",
            library = "ajv",
            schema = schemaJson
        });
    }

    public void PrepareGeneratedValidator(string modulePath)
    {
        Send(new
        {
            cmd = "prepare",
            library = "formfinch-js",
            modulePath
        });
    }

    public void PrepareData(string dataJson)
    {
        Send(new
        {
            cmd = "prepare-data",
            data = dataJson
        });
    }

    public int ValidatePreparedBatch(int iterations)
    {
        using var response = Send(new
        {
            cmd = "benchmark-prepared",
            iterations
        });

        if (!response.RootElement.TryGetProperty("ValidCount", out var validCount) ||
            !validCount.TryGetInt32(out var count))
        {
            throw new InvalidOperationException($"Node benchmark host did not return a ValidCount for batch size {iterations}.");
        }

        return count;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            if (!_process.HasExited)
            {
                try
                {
                    Send(new { cmd = "exit" }).Dispose();
                }
                catch
                {
                    // Best effort shutdown.
                }

                if (!_process.WaitForExit(2000))
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
        }
        finally
        {
            _stdin.Dispose();
            _stdout.Dispose();
            _process.Dispose();
        }
    }

    private JsonDocument Send(object command)
    {
        ThrowIfDisposed();

        var json = JsonSerializer.Serialize(command);
        _stdin.WriteLine(json);
        _stdin.Flush();

        var responseLine = _stdout.ReadLine();
        if (string.IsNullOrWhiteSpace(responseLine))
        {
            var stderr = _process.HasExited ? _process.StandardError.ReadToEnd() : string.Empty;
            throw new InvalidOperationException(
                $"Node benchmark host returned no response. Process exited: {_process.HasExited}. STDERR: {stderr}");
        }

        var response = JsonDocument.Parse(responseLine);
        if (!response.RootElement.TryGetProperty("Success", out var successElement) ||
            !successElement.GetBoolean())
        {
            var error = response.RootElement.TryGetProperty("Error", out var errorElement)
                ? errorElement.GetString()
                : "Unknown node benchmark host error.";
            response.Dispose();
            throw new InvalidOperationException(error);
        }

        return response;
    }

    private static string LocateAdapterHostPath()
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 12 && current != null; i++)
        {
            var candidate = Path.Combine(current, "benchmarks", "node", "adapter-host.js");
            if (File.Exists(candidate))
            {
                var nodeModules = Path.Combine(current, "benchmarks", "node", "node_modules");
                if (!Directory.Exists(nodeModules))
                {
                    throw new InvalidOperationException(
                        $"Node benchmark dependencies are missing at '{nodeModules}'. Run 'npm install' in benchmarks/node first.");
                }

                return candidate;
            }

            current = Path.GetDirectoryName(current);
        }

        throw new InvalidOperationException(
            "Could not locate benchmarks/node/adapter-host.js from the benchmark output directory.");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
