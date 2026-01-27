// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Config;

/// <summary>
/// Configuration for cold run benchmarks that measure first-run/startup costs.
/// Uses low invocation count and no unrolling to capture true cold performance.
/// </summary>
public sealed class ColdRunConfig : ManualConfig
{
    public ColdRunConfig()
    {
        AddJob(Job.Default
            .WithStrategy(RunStrategy.ColdStart)
            .WithInvocationCount(1)
            .WithUnrollFactor(1)
            .WithWarmupCount(0)
            .WithIterationCount(10));

        AddDiagnoser(MemoryDiagnoser.Default);

        AddExporter(MarkdownExporter.GitHub);
        AddExporter(JsonExporter.Full);

        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.P95);
        AddColumn(RankColumn.Arabic);

        Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
    }
}
