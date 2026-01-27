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
/// Standard benchmark configuration with memory diagnostics and markdown output.
/// </summary>
public sealed class DefaultConfig : ManualConfig
{
    public DefaultConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(3)
            .WithIterationCount(15));

        AddDiagnoser(MemoryDiagnoser.Default);

        AddExporter(MarkdownExporter.GitHub);
        AddExporter(JsonExporter.Full);

        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.P95);
        AddColumn(RankColumn.Arabic);

        Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
    }
}
