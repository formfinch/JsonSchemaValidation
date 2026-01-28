// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Config;

/// <summary>
/// Custom column that displays operations (validations) per second.
/// Calculated as 1,000,000,000 / mean_nanoseconds.
/// </summary>
public sealed class OperationsPerSecondColumn : IColumn
{
    public string Id => "Ops/sec";
    public string ColumnName => "Ops/sec";
    public string Legend => "Operations (validations) per second";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 0;
    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Dimensionless;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    public bool IsAvailable(Summary summary) => true;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        var report = summary[benchmarkCase];
        if (report?.ResultStatistics == null)
            return "N/A";

        var meanNs = report.ResultStatistics.Mean;
        if (meanNs <= 0)
            return "N/A";

        var opsPerSecond = 1_000_000_000.0 / meanNs;

        return opsPerSecond switch
        {
            >= 1_000_000 => $"{opsPerSecond / 1_000_000:N2}M",
            >= 1_000 => $"{opsPerSecond / 1_000:N1}K",
            _ => $"{opsPerSecond:N0}"
        };
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        => GetValue(summary, benchmarkCase);

    public override string ToString() => ColumnName;
}
