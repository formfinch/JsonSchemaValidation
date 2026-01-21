using System.Diagnostics;
using System.Text.Json;
using FormFinch.JsonSchemaValidationBenchmarks.Adapters;

namespace FormFinch.JsonSchemaValidationBenchmarks.Core;

public sealed class BenchmarkRunner
{
    private readonly BenchmarkOptions _options;

    public BenchmarkRunner(BenchmarkOptions options)
    {
        _options = options;
    }

    public BenchmarkResult Run(
        ISchemaValidatorAdapter adapter,
        string schemaJson,
        string dataJson,
        string scenarioId,
        string scenarioName)
    {
        if (_options.IncludeSchemaCompilation)
        {
            return RunWithCompilation(adapter, schemaJson, dataJson, scenarioId, scenarioName);
        }

        adapter.PrepareSchema(schemaJson);

        ForceGarbageCollection();

        // Pre-parse JSON for fair comparison (matches AJV benchmark methodology)
        using var jsonDoc = JsonDocument.Parse(dataJson);
        var jsonElement = jsonDoc.RootElement.Clone();

        // Check if adapter supports pre-parsed data
        var preparsedAdapter = adapter as IPreparsedSchemaValidatorAdapter;

        bool? validationResult = null;
        for (int i = 0; i < _options.WarmupIterations; i++)
        {
            validationResult = preparsedAdapter?.Validate(jsonElement) ?? adapter.Validate(dataJson);
        }

        var timings = new List<double>(_options.Iterations);
        long allocatedBefore = 0;
        int gen0Before = 0, gen1Before = 0, gen2Before = 0;

        if (_options.CollectMemoryMetrics)
        {
            allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            gen0Before = GC.CollectionCount(0);
            gen1Before = GC.CollectionCount(1);
            gen2Before = GC.CollectionCount(2);
        }

        var stopwatch = new Stopwatch();

        if (preparsedAdapter != null)
        {
            // Fair benchmark: validate pre-parsed data (no JSON parsing in loop)
            for (int i = 0; i < _options.Iterations; i++)
            {
                stopwatch.Restart();
                preparsedAdapter.Validate(jsonElement);
                stopwatch.Stop();
                timings.Add(stopwatch.Elapsed.TotalMicroseconds);
            }
        }
        else
        {
            // Fallback: include JSON parsing in each iteration
            for (int i = 0; i < _options.Iterations; i++)
            {
                stopwatch.Restart();
                adapter.Validate(dataJson);
                stopwatch.Stop();
                timings.Add(stopwatch.Elapsed.TotalMicroseconds);
            }
        }

        long memoryAllocated = 0;
        GCCollectionCounts? gcCounts = null;

        if (_options.CollectMemoryMetrics)
        {
            long allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);
            memoryAllocated = allocatedAfter - allocatedBefore;
            gcCounts = new GCCollectionCounts
            {
                Gen0 = GC.CollectionCount(0) - gen0Before,
                Gen1 = GC.CollectionCount(1) - gen1Before,
                Gen2 = GC.CollectionCount(2) - gen2Before
            };
        }

        return new BenchmarkResult
        {
            LibraryName = adapter.Name,
            Runtime = adapter.Runtime,
            ScenarioId = scenarioId,
            ScenarioName = scenarioName,
            Iterations = _options.Iterations,
            Timings = timings,
            MemoryAllocatedBytes = memoryAllocated,
            GCCollections = gcCounts,
            ValidationResult = validationResult
        };
    }

    private BenchmarkResult RunWithCompilation(
        ISchemaValidatorAdapter adapter,
        string schemaJson,
        string dataJson,
        string scenarioId,
        string scenarioName)
    {
        // Warmup with full compile+validate cycle
        bool? validationResult = null;
        for (int i = 0; i < _options.WarmupIterations; i++)
        {
            adapter.PrepareSchema(schemaJson);
            validationResult = adapter.Validate(dataJson);
        }

        ForceGarbageCollection();

        var timings = new List<double>(_options.Iterations);
        long allocatedBefore = 0;
        int gen0Before = 0, gen1Before = 0, gen2Before = 0;

        if (_options.CollectMemoryMetrics)
        {
            allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            gen0Before = GC.CollectionCount(0);
            gen1Before = GC.CollectionCount(1);
            gen2Before = GC.CollectionCount(2);
        }

        var stopwatch = new Stopwatch();

        for (int i = 0; i < _options.Iterations; i++)
        {
            stopwatch.Restart();
            adapter.PrepareSchema(schemaJson);
            adapter.Validate(dataJson);
            stopwatch.Stop();
            timings.Add(stopwatch.Elapsed.TotalMicroseconds);
        }

        long memoryAllocated = 0;
        GCCollectionCounts? gcCounts = null;

        if (_options.CollectMemoryMetrics)
        {
            long allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);
            memoryAllocated = allocatedAfter - allocatedBefore;
            gcCounts = new GCCollectionCounts
            {
                Gen0 = GC.CollectionCount(0) - gen0Before,
                Gen1 = GC.CollectionCount(1) - gen1Before,
                Gen2 = GC.CollectionCount(2) - gen2Before
            };
        }

        return new BenchmarkResult
        {
            LibraryName = adapter.Name,
            Runtime = adapter.Runtime,
            ScenarioId = scenarioId,
            ScenarioName = scenarioName,
            Iterations = _options.Iterations,
            Timings = timings,
            MemoryAllocatedBytes = memoryAllocated,
            GCCollections = gcCounts,
            ValidationResult = validationResult
        };
    }

    public async Task<BenchmarkResult> RunAsync(
        ISchemaValidatorAdapter adapter,
        string schemaJson,
        string dataJson,
        string scenarioId,
        string scenarioName)
    {
        if (adapter is IAsyncSchemaValidatorAdapter asyncAdapter)
        {
            return await RunAsyncAdapter(asyncAdapter, schemaJson, dataJson, scenarioId, scenarioName);
        }

        return await Task.Run(() => Run(adapter, schemaJson, dataJson, scenarioId, scenarioName));
    }

    private async Task<BenchmarkResult> RunAsyncAdapter(
        IAsyncSchemaValidatorAdapter adapter,
        string schemaJson,
        string dataJson,
        string scenarioId,
        string scenarioName)
    {
        IReadOnlyList<double> timings;
        bool? validationResult = null;

        if (_options.IncludeSchemaCompilation)
        {
            // Warmup with full compile+validate cycle
            for (int i = 0; i < _options.WarmupIterations; i++)
            {
                await adapter.PrepareSchemaAsync(schemaJson);
                validationResult = await adapter.ValidateAsync(dataJson);
            }

            timings = await adapter.RunBenchmarkWithCompilationAsync(schemaJson, dataJson, _options.Iterations);
        }
        else
        {
            await adapter.PrepareSchemaAsync(schemaJson);

            for (int i = 0; i < _options.WarmupIterations; i++)
            {
                validationResult = await adapter.ValidateAsync(dataJson);
            }

            timings = await adapter.RunBenchmarkAsync(dataJson, _options.Iterations);
        }

        return new BenchmarkResult
        {
            LibraryName = adapter.Name,
            Runtime = adapter.Runtime,
            ScenarioId = scenarioId,
            ScenarioName = scenarioName,
            Iterations = _options.Iterations,
            Timings = timings,
            MemoryAllocatedBytes = 0,
            GCCollections = null,
            ValidationResult = validationResult
        };
    }

    private static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
