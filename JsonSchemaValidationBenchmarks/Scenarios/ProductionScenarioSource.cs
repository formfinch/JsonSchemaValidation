using System.Text.Json;

namespace JsonSchemaValidationBenchmarks.Scenarios;

/// <summary>
/// Loads benchmark scenarios from real-world production schemas.
/// These are large, complex schemas used in actual production environments
/// to test validator performance at scale.
/// </summary>
public sealed class ProductionScenarioSource : IScenarioSource
{
    private readonly string _scenariosPath;
    private readonly Lazy<List<BenchmarkScenario>> _scenarios;

    public string Name => "production";

    // Production scenario definitions with metadata
    private static readonly ProductionScenarioDefinition[] ScenarioDefinitions =
    [
        new("cloudformation", "AWS CloudFormation", "infrastructure",
            "cloudformation-schema.json", "cloudformation-data.json"),
        new("github-workflow", "GitHub Workflow", "ci-cd",
            "github-workflow-schema.json", "github-workflow-data.json"),
        new("docker-compose", "Docker Compose", "infrastructure",
            "docker-compose-schema.json", "docker-compose-data.json"),
        new("package-json", "NPM package.json", "config",
            "package-json-schema.json", "package-json-data.json"),
        new("tsconfig", "TypeScript tsconfig", "config",
            "tsconfig-schema.json", "tsconfig-data.json"),
        new("eslintrc", "ESLint Configuration", "config",
            "eslintrc-schema.json", "eslintrc-data.json")
    ];

    public ProductionScenarioSource(string scenariosPath)
    {
        _scenariosPath = scenariosPath;
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
        var scenarios = new List<BenchmarkScenario>();
        var productionPath = Path.Combine(_scenariosPath, "production");

        if (!Directory.Exists(productionPath))
        {
            Console.WriteLine($"Warning: Production scenarios path not found: {productionPath}");
            return scenarios;
        }

        foreach (var def in ScenarioDefinitions)
        {
            var schemaPath = Path.Combine(productionPath, def.SchemaFile);
            var dataPath = Path.Combine(productionPath, def.DataFile);

            if (!File.Exists(schemaPath))
            {
                Console.WriteLine($"Warning: Schema file not found: {schemaPath}");
                continue;
            }

            if (!File.Exists(dataPath))
            {
                Console.WriteLine($"Warning: Data file not found: {dataPath}");
                continue;
            }

            try
            {
                var schemaJson = File.ReadAllText(schemaPath);
                var dataJson = File.ReadAllText(dataPath);

                // Validate that both are valid JSON
                using (JsonDocument.Parse(schemaJson)) { }
                using (JsonDocument.Parse(dataJson)) { }

                var schemaSize = new FileInfo(schemaPath).Length;
                var sizeLabel = schemaSize switch
                {
                    > 1_000_000 => $"{schemaSize / 1_000_000.0:F1}MB",
                    > 1_000 => $"{schemaSize / 1_000.0:F0}KB",
                    _ => $"{schemaSize}B"
                };

                scenarios.Add(new BenchmarkScenario
                {
                    Id = $"prod-{def.Id}",
                    Name = $"{def.Name} ({sizeLabel} schema)",
                    Category = def.Category,
                    SchemaJson = schemaJson,
                    TestCases =
                    [
                        new ScenarioTestCase
                        {
                            Name = "valid",
                            DataJson = dataJson,
                            ExpectedValid = true
                        }
                    ]
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load {def.Name}: {ex.Message}");
            }
        }

        return scenarios;
    }

    private sealed record ProductionScenarioDefinition(
        string Id,
        string Name,
        string Category,
        string SchemaFile,
        string DataFile);
}
