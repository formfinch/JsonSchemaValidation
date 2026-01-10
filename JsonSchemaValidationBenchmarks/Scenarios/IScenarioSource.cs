namespace JsonSchemaValidationBenchmarks.Scenarios;

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
