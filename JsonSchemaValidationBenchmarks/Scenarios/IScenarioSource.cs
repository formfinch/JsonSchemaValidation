// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidationBenchmarks.Scenarios;

public interface IScenarioSource
{
    string Name { get; }

    IEnumerable<BenchmarkScenario> GetScenarios();

    IEnumerable<BenchmarkScenario> GetScenarios(string? category);
}

public sealed class BenchmarkScenario
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Category { get; init; }

    public required string SchemaJson { get; init; }

    public required IReadOnlyList<ScenarioTestCase> TestCases { get; init; }
}

public sealed class ScenarioTestCase
{
    public required string Name { get; init; }

    public required string DataJson { get; init; }

    public required bool ExpectedValid { get; init; }
}
