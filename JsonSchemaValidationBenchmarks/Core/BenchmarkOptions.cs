namespace FormFinch.JsonSchemaValidationBenchmarks.Core;

public sealed class BenchmarkOptions
{
    public int WarmupIterations { get; set; } = 100;

    public int Iterations { get; set; } = 1000;

    public bool CollectMemoryMetrics { get; set; } = true;

    /// <summary>
    /// When true, measures full end-to-end time (schema compilation + validation) per iteration.
    /// This provides a fairer comparison between JIT-compiled validators (like Ajv) and interpreted ones.
    /// </summary>
    public bool IncludeSchemaCompilation { get; set; }

    public static BenchmarkOptions Default => new();

    public static BenchmarkOptions Quick => new()
    {
        WarmupIterations = 20,
        Iterations = 100,
        CollectMemoryMetrics = true
    };
}
