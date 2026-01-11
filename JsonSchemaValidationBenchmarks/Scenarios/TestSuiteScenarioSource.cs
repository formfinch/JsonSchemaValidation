using System.Text.Json;

namespace JsonSchemaValidationBenchmarks.Scenarios;

public sealed class TestSuiteScenarioSource : IScenarioSource
{
    private readonly string _testSuitePath;
    private readonly string _draftVersion;
    private readonly Lazy<List<BenchmarkScenario>> _scenarios;

    public string Name => $"testsuite-{_draftVersion}";

    private static readonly Dictionary<string, string> CategoryMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["type"] = "core",
        ["enum"] = "core",
        ["const"] = "core",
        ["properties"] = "core",
        ["required"] = "core",
        ["additionalProperties"] = "core",
        ["ref"] = "core",
        ["defs"] = "core",
        ["anchor"] = "core",
        ["id"] = "core",
        ["allOf"] = "complex",
        ["anyOf"] = "complex",
        ["oneOf"] = "complex",
        ["not"] = "complex",
        ["if-then-else"] = "complex",
        ["dependentSchemas"] = "complex",
        ["dependentRequired"] = "complex",
        ["unevaluatedProperties"] = "complex",
        ["unevaluatedItems"] = "complex",
        ["dynamicRef"] = "complex",
        ["items"] = "array",
        ["prefixItems"] = "array",
        ["contains"] = "array",
        ["minItems"] = "array",
        ["maxItems"] = "array",
        ["uniqueItems"] = "array",
        ["minContains"] = "array",
        ["maxContains"] = "array",
        ["pattern"] = "string",
        ["minLength"] = "string",
        ["maxLength"] = "string",
        ["format"] = "format",
        ["minimum"] = "numeric",
        ["maximum"] = "numeric",
        ["exclusiveMinimum"] = "numeric",
        ["exclusiveMaximum"] = "numeric",
        ["multipleOf"] = "numeric",
        ["minProperties"] = "object",
        ["maxProperties"] = "object",
        ["patternProperties"] = "object",
        ["propertyNames"] = "object"
    };

    /// <summary>
    /// Creates a test suite scenario source for the specified draft version.
    /// </summary>
    /// <param name="testSuitePath">Path to the JSON-Schema-Test-Suite root</param>
    /// <param name="draftVersion">Draft version folder name (e.g., "draft2020-12" or "draft2019-09")</param>
    public TestSuiteScenarioSource(string testSuitePath, string draftVersion = "draft2020-12")
    {
        _testSuitePath = testSuitePath;
        _draftVersion = draftVersion;
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

    // Files that use custom metaschemas, remote schemas, or test optional features not widely supported
    private static readonly HashSet<string> ExcludedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "vocabulary", // Tests custom metaschemas with non-standard $vocabulary - optional feature
        "refRemote"   // Tests remote schema resolution - requires localhost:1234 server or pre-loaded schemas
    };

    private List<BenchmarkScenario> LoadScenarios()
    {
        var scenarios = new List<BenchmarkScenario>();
        var testsPath = Path.Combine(_testSuitePath, "tests", _draftVersion);

        if (!Directory.Exists(testsPath))
        {
            return scenarios;
        }

        foreach (var file in Directory.GetFiles(testsPath, "*.json")
            .Where(f => !ExcludedFiles.Contains(Path.GetFileNameWithoutExtension(f))))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var category = GetCategory(fileName);

            try
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);

                foreach (var testGroup in doc.RootElement.EnumerateArray())
                {
                    var description = testGroup.GetProperty("description").GetString()!;
                    var schema = testGroup.GetProperty("schema");
                    var schemaJson = schema.GetRawText();
                    var tests = testGroup.GetProperty("tests");

                    var testCases = new List<ScenarioTestCase>();
                    foreach (var test in tests.EnumerateArray())
                    {
                        var testDesc = test.GetProperty("description").GetString()!;
                        var data = test.GetProperty("data");
                        var valid = test.GetProperty("valid").GetBoolean();

                        testCases.Add(new ScenarioTestCase
                        {
                            Name = testDesc,
                            DataJson = data.GetRawText(),
                            ExpectedValid = valid
                        });
                    }

                    // Skip scenarios that reference remote schemas (localhost:1234)
                    // These require a running server or pre-loaded schemas
                    if (testCases.Count > 0 && !schemaJson.Contains("localhost:1234"))
                    {
                        var id = $"{fileName}-{SanitizeId(description)}";
                        scenarios.Add(new BenchmarkScenario
                        {
                            Id = id,
                            Name = $"{fileName}: {description}",
                            Category = category,
                            SchemaJson = schemaJson,
                            TestCases = testCases
                        });
                    }
                }
            }
            catch
            {
                // Skip files that can't be parsed
            }
        }

        return scenarios;
    }

    private static string GetCategory(string fileName)
    {
        return CategoryMappings.TryGetValue(fileName, out var category)
            ? category
            : "other";
    }

    private static string SanitizeId(string description)
    {
        return description
            .ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('/', '-')
            .Replace('\\', '-')
            .Replace('$', '-')
            .Replace('#', '-');
    }
}
