// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Config;

/// <summary>
/// Configuration for throughput benchmarks with high iteration counts.
/// Optimized for measuring sustained throughput rather than single-call latency.
/// </summary>
public sealed class ThroughputConfig : ManualConfig
{
    public ThroughputConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(5)
            .WithIterationCount(20));

        AddDiagnoser(MemoryDiagnoser.Default);

        AddExporter(MarkdownExporter.GitHub);
        AddExporter(JsonExporter.Full);

        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.P95);
        AddColumn(StatisticColumn.OperationsPerSecond);
        AddColumn(RankColumn.Arabic);

        Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
    }
}
