// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Config;

/// <summary>
/// Quick benchmark configuration for fast performance sanity checks.
/// Uses minimal warmup and iterations to complete in under a minute.
/// Not suitable for accurate measurements - use DefaultConfig for that.
/// </summary>
public sealed class QuickConfig : ManualConfig
{
    public QuickConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(1)
            .WithIterationCount(3));

        AddDiagnoser(MemoryDiagnoser.Default);

        AddColumn(StatisticColumn.Median);
        AddColumn(new OperationsPerSecondColumn());
        AddColumn(RankColumn.Arabic);

        Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
    }
}
