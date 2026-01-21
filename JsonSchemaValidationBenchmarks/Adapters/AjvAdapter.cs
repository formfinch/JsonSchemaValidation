using FormFinch.JsonSchemaValidationBenchmarks.NodeHost;

namespace FormFinch.JsonSchemaValidationBenchmarks.Adapters;

public sealed class AjvAdapter : IAsyncSchemaValidatorAdapter
{
    public string Name => "Ajv";
    public string Runtime => "node";

    private readonly NodeProcessHost _host;
    private readonly string _scriptPath;
    private bool _started;
    private string? _currentSchema;

    public AjvAdapter(string benchmarksPath)
    {
        _host = new NodeProcessHost();
        _scriptPath = Path.Combine(benchmarksPath, "node", "adapter-host.js");
    }

    private async Task EnsureStartedAsync()
    {
        if (!_started)
        {
            await _host.StartAsync(_scriptPath);
            _started = true;
        }
    }

    public void PrepareSchema(string schemaJson)
    {
        PrepareSchemaAsync(schemaJson).GetAwaiter().GetResult();
    }

    public async Task PrepareSchemaAsync(string schemaJson)
    {
        await EnsureStartedAsync();

        var response = await _host.SendCommandAsync(new NodeCommand
        {
            Cmd = "prepare",
            Library = "ajv",
            Schema = schemaJson
        });

        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to prepare schema: {response.Error}");
        }

        _currentSchema = schemaJson;
    }

    public bool Validate(string dataJson)
    {
        return ValidateAsync(dataJson).GetAwaiter().GetResult();
    }

    public async Task<bool> ValidateAsync(string dataJson)
    {
        await EnsureStartedAsync();

        var response = await _host.SendCommandAsync(new NodeCommand
        {
            Cmd = "validate",
            Data = dataJson
        });

        if (!response.Success)
        {
            throw new InvalidOperationException($"Validation failed: {response.Error}");
        }

        return response.Valid ?? false;
    }

    public async Task<IReadOnlyList<double>> RunBenchmarkAsync(string dataJson, int iterations)
    {
        await EnsureStartedAsync();

        var response = await _host.SendCommandAsync(new NodeCommand
        {
            Cmd = "benchmark",
            Data = dataJson,
            Iterations = iterations
        });

        if (!response.Success)
        {
            throw new InvalidOperationException($"Benchmark failed: {response.Error}");
        }

        return response.Timings ?? Array.Empty<double>();
    }

    public async Task<IReadOnlyList<double>> RunBenchmarkWithCompilationAsync(string schemaJson, string dataJson, int iterations)
    {
        await EnsureStartedAsync();

        var response = await _host.SendCommandAsync(new NodeCommand
        {
            Cmd = "benchmark-full",
            Library = "ajv",
            Schema = schemaJson,
            Data = dataJson,
            Iterations = iterations
        });

        if (!response.Success)
        {
            throw new InvalidOperationException($"Benchmark failed: {response.Error}");
        }

        return response.Timings ?? Array.Empty<double>();
    }

    public void Dispose()
    {
        _host.Dispose();
    }
}
