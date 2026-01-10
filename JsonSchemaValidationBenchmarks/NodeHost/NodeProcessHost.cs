using System.Diagnostics;
using System.Text.Json;

namespace JsonSchemaValidationBenchmarks.NodeHost;

public sealed class NodeProcessHost : IDisposable
{
    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private bool _isRunning;

    public async Task StartAsync(string scriptPath)
    {
        if (_isRunning)
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = scriptPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _process = new Process { StartInfo = startInfo };
        _process.Start();

        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;
        _isRunning = true;

        var ready = await _stdout.ReadLineAsync();
        if (ready != "ready")
        {
            throw new InvalidOperationException($"Node process did not start correctly. Got: {ready}");
        }
    }

    public async Task<NodeResponse> SendCommandAsync(NodeCommand command)
    {
        if (!_isRunning)
        {
            throw new InvalidOperationException("Node process is not running");
        }

        var json = JsonSerializer.Serialize(command);
        await _stdin!.WriteLineAsync(json);
        await _stdin.FlushAsync();

        var responseLine = await _stdout!.ReadLineAsync();
        if (string.IsNullOrEmpty(responseLine))
        {
            throw new InvalidOperationException("No response from Node process");
        }

        return JsonSerializer.Deserialize<NodeResponse>(responseLine)
            ?? throw new InvalidOperationException("Failed to parse Node response");
    }

    public void Dispose()
    {
        _isRunning = false;
        _stdin?.Dispose();
        _stdout?.Dispose();

        if (_process is not null && !_process.HasExited)
        {
            _process.Kill();
            _process.Dispose();
        }
    }
}

public sealed class NodeCommand
{
    public string Cmd { get; set; } = string.Empty;
    public string? Library { get; set; }
    public string? Schema { get; set; }
    public string? Data { get; set; }
    public int? Iterations { get; set; }
}

public sealed class NodeResponse
{
    public bool Success { get; set; }
    public bool? Valid { get; set; }
    public double[]? Timings { get; set; }
    public string? Error { get; set; }
}
