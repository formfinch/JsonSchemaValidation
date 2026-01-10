using System.Text.Json.Serialization;

namespace JsonSchemaValidationBenchmarks.Core;

public sealed class BenchmarkResult
{
    public required string LibraryName { get; init; }

    public required string Runtime { get; init; }

    public required string ScenarioId { get; init; }

    public required string ScenarioName { get; init; }

    public required int Iterations { get; init; }

    public required IReadOnlyList<double> Timings { get; init; }

    public long MemoryAllocatedBytes { get; init; }

    public GCCollectionCounts? GCCollections { get; init; }

    public bool? ValidationResult { get; init; }

    [JsonIgnore]
    public double MedianMicroseconds => BenchmarkStatistics.Median(Timings);

    [JsonIgnore]
    public double MeanMicroseconds => BenchmarkStatistics.Mean(Timings);

    [JsonIgnore]
    public double StdDevMicroseconds => BenchmarkStatistics.StdDev(Timings);

    [JsonIgnore]
    public double MinMicroseconds => BenchmarkStatistics.Min(Timings);

    [JsonIgnore]
    public double MaxMicroseconds => BenchmarkStatistics.Max(Timings);

    [JsonIgnore]
    public double ThroughputPerSecond => MeanMicroseconds > 0 ? 1_000_000.0 / MeanMicroseconds : 0;

    [JsonIgnore]
    public double MemoryAllocatedKB => MemoryAllocatedBytes / 1024.0;

    public BenchmarkStats ToStats() => new()
    {
        MedianUs = MedianMicroseconds,
        MeanUs = MeanMicroseconds,
        StdDevUs = StdDevMicroseconds,
        MinUs = MinMicroseconds,
        MaxUs = MaxMicroseconds,
        ThroughputPerSec = ThroughputPerSecond,
        MemoryAllocatedKB = MemoryAllocatedKB
    };
}

public sealed class BenchmarkStats
{
    public double MedianUs { get; init; }

    public double MeanUs { get; init; }

    public double StdDevUs { get; init; }

    public double MinUs { get; init; }

    public double MaxUs { get; init; }

    public double ThroughputPerSec { get; init; }

    public double MemoryAllocatedKB { get; init; }
}

public sealed class GCCollectionCounts
{
    public int Gen0 { get; init; }

    public int Gen1 { get; init; }

    public int Gen2 { get; init; }
}
