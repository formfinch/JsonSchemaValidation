using System.Text.Json;

namespace FormFinch.JsonSchemaValidationBenchmarks.Scenarios;

public sealed class ManifestScenarioSource : IScenarioSource
{
    public string Name => "manifest";

    private readonly string _benchmarksPath;
    private readonly Lazy<List<BenchmarkScenario>> _scenarios;

    public ManifestScenarioSource(string benchmarksPath)
    {
        _benchmarksPath = benchmarksPath;
        _scenarios = new Lazy<List<BenchmarkScenario>>(LoadScenarios);
    }

    public IEnumerable<BenchmarkScenario> GetScenarios() => _scenarios.Value;

    public IEnumerable<BenchmarkScenario> GetScenarios(string? category)
    {
        if (string.IsNullOrEmpty(category))
        {
            return GetScenarios();
        }

        return _scenarios.Value.Where(s =>
            s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    private List<BenchmarkScenario> LoadScenarios()
    {
        var manifestPath = Path.Combine(_benchmarksPath, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return new List<BenchmarkScenario>();
        }

        var json = File.ReadAllText(manifestPath);
        using var doc = JsonDocument.Parse(json);

        var scenarios = new List<BenchmarkScenario>();
        var root = doc.RootElement;

        if (!root.TryGetProperty("scenarios", out var scenariosArray))
        {
            return scenarios;
        }

        foreach (var scenarioEl in scenariosArray.EnumerateArray())
        {
            var id = scenarioEl.GetProperty("id").GetString()!;
            var name = scenarioEl.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString()!
                : id;
            var category = scenarioEl.GetProperty("category").GetString()!;
            var schemaPath = scenarioEl.GetProperty("schema").GetString()!;

            var schemaFullPath = Path.Combine(_benchmarksPath, schemaPath);
            if (!File.Exists(schemaFullPath))
            {
                continue;
            }

            var schemaJson = File.ReadAllText(schemaFullPath);
            var testCases = new List<ScenarioTestCase>();

            if (scenarioEl.TryGetProperty("validData", out var validDataProp))
            {
                var validDataPath = Path.Combine(_benchmarksPath, validDataProp.GetString()!);
                if (File.Exists(validDataPath))
                {
                    testCases.Add(new ScenarioTestCase
                    {
                        Name = "valid",
                        DataJson = File.ReadAllText(validDataPath),
                        ExpectedValid = true
                    });
                }
            }

            if (scenarioEl.TryGetProperty("invalidData", out var invalidDataProp))
            {
                var invalidDataPath = Path.Combine(_benchmarksPath, invalidDataProp.GetString()!);
                if (File.Exists(invalidDataPath))
                {
                    testCases.Add(new ScenarioTestCase
                    {
                        Name = "invalid",
                        DataJson = File.ReadAllText(invalidDataPath),
                        ExpectedValid = false
                    });
                }
            }

            if (testCases.Count > 0)
            {
                scenarios.Add(new BenchmarkScenario
                {
                    Id = id,
                    Name = name,
                    Category = category,
                    SchemaJson = schemaJson,
                    TestCases = testCases
                });
            }
        }

        return scenarios;
    }
}
