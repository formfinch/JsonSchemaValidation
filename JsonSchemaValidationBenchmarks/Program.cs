using System.CommandLine;
using System.Text.Json;
using JsonSchemaValidationBenchmarks.Adapters;
using JsonSchemaValidationBenchmarks.Core;
using JsonSchemaValidationBenchmarks.Scenarios;

var rootCommand = new RootCommand("JSON Schema Validation Benchmark Suite");

var librariesOption = new Option<string[]>(
    name: "--libraries",
    getDefaultValue: () => new[] { "jsonschemavalidation" },
    description: "Libraries to benchmark: jsonschemavalidation, jsv2020, jsv2019, jsv7, jsv6, jsv4, jsv3, jsonschemanet, njsonschema, lateapex, ajv, hyperjump, cfworker. Use jsv2020,jsv2019,jsv7,jsv6,jsv4,jsv3 to compare draft performance.");
librariesOption.AddAlias("-l");

var scenariosOption = new Option<string[]>(
    name: "--scenarios",
    getDefaultValue: () => new[] { "testsuite" },
    description: "Scenario sources: manifest, testsuite (draft2020-12), testsuite-2019-09, testsuite-7, testsuite-6, or specific scenario IDs");
scenariosOption.AddAlias("-s");

var categoriesOption = new Option<string[]>(
    name: "--categories",
    getDefaultValue: () => Array.Empty<string>(),
    description: "Filter by category (core, complex, format, array, string, numeric, object)");
categoriesOption.AddAlias("-c");

var iterationsOption = new Option<int>(
    name: "--iterations",
    getDefaultValue: () => 1000,
    description: "Number of measurement iterations per scenario");
iterationsOption.AddAlias("-i");

var warmupOption = new Option<int>(
    name: "--warmup",
    getDefaultValue: () => 100,
    description: "Number of warmup iterations");
warmupOption.AddAlias("-w");

var outputOption = new Option<string?>(
    name: "--output",
    getDefaultValue: () => null,
    description: "Output file path for JSON results");
outputOption.AddAlias("-o");

var quickOption = new Option<bool>(
    name: "--quick",
    getDefaultValue: () => false,
    description: "Quick mode: runs core scenarios with reduced iterations for fast feedback");
quickOption.AddAlias("-q");

var verboseOption = new Option<bool>(
    name: "--verbose",
    getDefaultValue: () => false,
    description: "Show detailed progress output");
verboseOption.AddAlias("-v");

var includeCompileOption = new Option<bool>(
    name: "--include-compile",
    getDefaultValue: () => false,
    description: "Include schema compilation time in each iteration (fairer comparison for JIT-compiled validators like Ajv)");
includeCompileOption.AddAlias("-f");

rootCommand.AddOption(librariesOption);
rootCommand.AddOption(scenariosOption);
rootCommand.AddOption(categoriesOption);
rootCommand.AddOption(iterationsOption);
rootCommand.AddOption(warmupOption);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(quickOption);
rootCommand.AddOption(verboseOption);
rootCommand.AddOption(includeCompileOption);

rootCommand.SetHandler(async (context) =>
{
    var libraries = context.ParseResult.GetValueForOption(librariesOption)!;
    var scenarios = context.ParseResult.GetValueForOption(scenariosOption)!;
    var categories = context.ParseResult.GetValueForOption(categoriesOption)!;
    var iterations = context.ParseResult.GetValueForOption(iterationsOption);
    var warmup = context.ParseResult.GetValueForOption(warmupOption);
    var output = context.ParseResult.GetValueForOption(outputOption);
    var quick = context.ParseResult.GetValueForOption(quickOption);
    var verbose = context.ParseResult.GetValueForOption(verboseOption);
    var includeCompile = context.ParseResult.GetValueForOption(includeCompileOption);

    if (quick)
    {
        iterations = 100;
        warmup = 20;
        categories = new[] { "core" };
        if (libraries.Length == 1 && libraries[0] == "jsonschemavalidation")
        {
            // Default quick mode just runs our library
        }
    }

    var benchmarkOptions = new BenchmarkOptions
    {
        Iterations = iterations,
        WarmupIterations = warmup,
        CollectMemoryMetrics = true,
        IncludeSchemaCompilation = includeCompile
    };

    await RunBenchmarks(libraries, scenarios, categories, benchmarkOptions, output, verbose);
});

return await rootCommand.InvokeAsync(args);

static async Task RunBenchmarks(
    string[] libraries,
    string[] scenarioSources,
    string[] categories,
    BenchmarkOptions options,
    string? outputPath,
    bool verbose)
{
    var basePath = AppContext.BaseDirectory;
    var benchmarksPath = Path.Combine(basePath, "benchmarks");
    var testSuitePath = FindTestSuitePath(basePath);

    var allScenarios = LoadScenarios(scenarioSources, benchmarksPath, testSuitePath, categories);

    if (allScenarios.Count == 0)
    {
        Console.WriteLine("No scenarios found to benchmark.");
        return;
    }

    Console.WriteLine($"=== JSON Schema Validation Benchmarks ===");
    Console.WriteLine($"Libraries: {string.Join(", ", libraries)}");
    Console.WriteLine($"Scenarios: {allScenarios.Count}");
    Console.WriteLine($"Iterations: {options.Iterations} (warmup: {options.WarmupIterations})");
    Console.WriteLine();

    var results = new List<BenchmarkResult>();
    var runner = new BenchmarkRunner(options);

    foreach (var libraryName in libraries)
    {
        using var adapter = CreateAdapter(libraryName, benchmarksPath);
        if (adapter is null)
        {
            Console.WriteLine($"Unknown library: {libraryName}");
            continue;
        }

        Console.WriteLine($"[{adapter.Name}]");

        foreach (var scenario in allScenarios)
        {
            if (verbose)
            {
                Console.WriteLine($"  Running: {scenario.Name}");
            }

            foreach (var testCase in scenario.TestCases)
            {
                var scenarioId = $"{scenario.Id}-{testCase.Name}";
                var scenarioName = $"{scenario.Name} ({testCase.Name})";

                try
                {
                    BenchmarkResult result;
                    if (adapter is IAsyncSchemaValidatorAdapter asyncAdapter)
                    {
                        result = await runner.RunAsync(
                            asyncAdapter, scenario.SchemaJson, testCase.DataJson, scenarioId, scenarioName);
                    }
                    else
                    {
                        result = runner.Run(
                            adapter, scenario.SchemaJson, testCase.DataJson, scenarioId, scenarioName);
                    }

                    results.Add(result);

                    if (verbose)
                    {
                        Console.WriteLine($"    {testCase.Name}: {result.MedianMicroseconds:F2} us (valid={result.ValidationResult})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Error in {scenarioId}: {ex.Message}");
                }
            }
        }

        Console.WriteLine();
    }

    PrintSummary(results);

    if (!string.IsNullOrEmpty(outputPath))
    {
        await SaveResults(results, outputPath);
        Console.WriteLine($"Results saved to: {outputPath}");
    }
}

static List<BenchmarkScenario> LoadScenarios(
    string[] sources,
    string benchmarksPath,
    string testSuitePath,
    string[] categories)
{
    var scenarios = new List<BenchmarkScenario>();
    var categoryFilter = categories.Length > 0 ? categories : null;

    foreach (var source in sources)
    {
        IScenarioSource? scenarioSource = source.ToLowerInvariant() switch
        {
            "manifest" => new ManifestScenarioSource(benchmarksPath),
            "testsuite" => new TestSuiteScenarioSource(testSuitePath, "draft2020-12"),
            "testsuite-2019-09" => new TestSuiteScenarioSource(testSuitePath, "draft2019-09"),
            "testsuite-7" or "testsuite-draft7" => new TestSuiteScenarioSource(testSuitePath, "draft7"),
            "testsuite-6" or "testsuite-draft6" => new TestSuiteScenarioSource(testSuitePath, "draft6"),
            _ => null
        };

        if (scenarioSource is not null)
        {
            var sourceScenarios = categoryFilter is not null
                ? categoryFilter.SelectMany(c => scenarioSource.GetScenarios(c))
                : scenarioSource.GetScenarios();
            scenarios.AddRange(sourceScenarios);
        }
    }

    return scenarios.DistinctBy(s => s.Id).ToList();
}

static ISchemaValidatorAdapter? CreateAdapter(string name, string benchmarksPath) =>
    name.ToLowerInvariant() switch
    {
        "jsonschemavalidation" => new JsonSchemaValidationAdapter(),
        "jsv2020" => new JsonSchemaValidation2020Adapter(),
        "jsv2019" => new JsonSchemaValidation2019Adapter(),
        "jsv7" or "jsvdraft7" => new JsonSchemaValidation7Adapter(),
        "jsv6" or "jsvdraft6" => new JsonSchemaValidation6Adapter(),
        "jsv4" or "jsvdraft4" => new JsonSchemaValidation4Adapter(),
        "jsv3" or "jsvdraft3" => new JsonSchemaValidation3Adapter(),
        "jsonschemanet" => new JsonSchemaNetAdapter(),
        "njsonschema" => new NJsonSchemaAdapter(),
        "lateapex" => new LateApexAdapter(),
        "ajv" => new AjvAdapter(benchmarksPath),
        "hyperjump" => new HyperjumpAdapter(benchmarksPath),
        "cfworker" => new CfworkerAdapter(benchmarksPath),
        _ => null
    };

static string FindTestSuitePath(string basePath)
{
    var current = basePath;
    for (int i = 0; i < 10; i++)
    {
        var testSuitePath = Path.Combine(current, "submodules", "JSON-Schema-Test-Suite");
        if (Directory.Exists(testSuitePath))
        {
            return testSuitePath;
        }
        var parent = Directory.GetParent(current);
        if (parent is null) break;
        current = parent.FullName;
    }
    return Path.Combine(basePath, "submodules", "JSON-Schema-Test-Suite");
}

static void PrintSummary(List<BenchmarkResult> results)
{
    if (results.Count == 0)
    {
        Console.WriteLine("No results to display.");
        return;
    }

    var libraries = results.Select(r => r.LibraryName).Distinct().OrderBy(n => n).ToList();
    var scenarios = results.GroupBy(r => r.ScenarioId).OrderBy(g => g.Key).ToList();

    // Print comparison table if multiple libraries
    if (libraries.Count > 1)
    {
        PrintComparisonTable(results, libraries, scenarios);
    }
    else
    {
        PrintSingleLibraryTable(results, scenarios);
    }

    // Print overall summary
    PrintOverallSummary(results, libraries);
}

static void PrintComparisonTable(
    List<BenchmarkResult> results,
    List<string> libraries,
    List<IGrouping<string, BenchmarkResult>> scenarios)
{
    Console.WriteLine();
    Console.WriteLine("=== PERFORMANCE COMPARISON ===");
    Console.WriteLine();

    // Build header with fixed-width columns
    const int colWidth = 16;
    Console.Write($"{"Scenario",-45}");
    foreach (var lib in libraries)
    {
        var shortName = GetShortLibraryName(lib);
        Console.Write($"{shortName,colWidth}");
    }
    Console.WriteLine($"{"Winner",10}");
    Console.WriteLine(new string('-', 45 + libraries.Count * colWidth + 10));

    foreach (var group in scenarios)
    {
        var scenarioResults = group.ToDictionary(r => r.LibraryName);
        var fastest = group.MinBy(r => r.MedianMicroseconds);
        if (fastest is null) continue;

        // Truncate scenario name
        var scenarioName = group.First().ScenarioName;
        if (scenarioName.Length > 43)
            scenarioName = scenarioName[..40] + "...";

        Console.Write($"{scenarioName,-45}");

        foreach (var lib in libraries)
        {
            if (scenarioResults.TryGetValue(lib, out var result))
            {
                var timeStr = FormatMicroseconds(result.MedianMicroseconds);
                var relativeSpeed = result.MedianMicroseconds / fastest.MedianMicroseconds;

                if (relativeSpeed <= 1.1)
                    Console.Write($"{timeStr,colWidth}");
                else
                    Console.Write($"{timeStr,9} {relativeSpeed,5:F1}x");
            }
            else
            {
                Console.Write($"{"--",colWidth}");
            }
        }

        Console.WriteLine($"{GetShortLibraryName(fastest.LibraryName),10}");
    }
}

static void PrintSingleLibraryTable(List<BenchmarkResult> results, List<IGrouping<string, BenchmarkResult>> scenarios)
{
    Console.WriteLine();
    Console.WriteLine($"=== BENCHMARK RESULTS: {results.First().LibraryName} ===");
    Console.WriteLine();
    Console.WriteLine($"{"Scenario",-50} {"Median",10} {"Memory",10} {"Throughput",12}");
    Console.WriteLine(new string('-', 85));

    foreach (var group in scenarios)
    {
        foreach (var result in group)
        {
            var scenarioName = result.ScenarioName;
            if (scenarioName.Length > 48)
                scenarioName = scenarioName[..45] + "...";

            var timeStr = FormatMicroseconds(result.MedianMicroseconds);
            var memStr = result.MemoryAllocatedKB > 0 ? $"{result.MemoryAllocatedKB:F1} KB" : "--";
            var throughputStr = FormatThroughput(result.ThroughputPerSecond);

            Console.WriteLine($"{scenarioName,-50} {timeStr,10} {memStr,10} {throughputStr,12}");
        }
    }
}

static void PrintOverallSummary(List<BenchmarkResult> results, List<string> libraries)
{
    Console.WriteLine();
    Console.WriteLine("=== SUMMARY ===");
    Console.WriteLine();

    foreach (var lib in libraries)
    {
        var libResults = results.Where(r => r.LibraryName == lib).ToList();
        if (libResults.Count == 0) continue;

        var medians = libResults.Select(r => r.MedianMicroseconds).ToList();
        var avgMedian = medians.Average();
        var minMedian = medians.Min();
        var maxMedian = medians.Max();
        // Calculate throughput from average time (not arithmetic mean of throughputs)
        var avgThroughput = avgMedian > 0 ? 1_000_000.0 / avgMedian : 0;
        var avgMemory = libResults.Average(r => r.MemoryAllocatedKB);

        Console.WriteLine($"{lib}:");
        Console.WriteLine($"  Scenarios: {libResults.Count}");
        Console.WriteLine($"  Median time: {FormatMicroseconds(avgMedian)} avg ({FormatMicroseconds(minMedian)} - {FormatMicroseconds(maxMedian)})");
        Console.WriteLine($"  Throughput: {FormatThroughput(avgThroughput)} avg");
        if (avgMemory > 0)
            Console.WriteLine($"  Memory: {avgMemory:F1} KB avg");
        Console.WriteLine();
    }

    // Win count if multiple libraries
    if (libraries.Count > 1)
    {
        var winCounts = results
            .GroupBy(r => r.ScenarioId)
            .Select(g => g.MinBy(r => r.MedianMicroseconds)?.LibraryName)
            .Where(n => n is not null)
            .GroupBy(n => n!)
            .ToDictionary(g => g.Key, g => g.Count());

        Console.WriteLine("Wins by library:");
        foreach (var lib in libraries.OrderByDescending(l => winCounts.GetValueOrDefault(l, 0)))
        {
            var wins = winCounts.GetValueOrDefault(lib, 0);
            var pct = results.GroupBy(r => r.ScenarioId).Count() > 0
                ? (double)wins / results.GroupBy(r => r.ScenarioId).Count() * 100
                : 0;
            Console.WriteLine($"  {GetShortLibraryName(lib),-20} {wins,4} wins ({pct:F0}%)");
        }
    }
}

static string GetShortLibraryName(string name) => name switch
{
    "JsonSchemaValidation" => "JSV",
    "JSV-2020-12" => "2020-12",
    "JSV-2019-09" => "2019-09",
    "JSV-Draft7" => "Draft7",
    "JSV-Draft6" => "Draft6",
    "JSV-Draft4" => "Draft4",
    "JSV-Draft3" => "Draft3",
    "JsonSchema.Net" => "JS.Net",
    "NJsonSchema" => "NJS",
    "LateApex" => "LateApex",
    "Ajv" => "Ajv",
    "Hyperjump" => "HJ",
    "cfworker" => "CFW",
    _ => name.Length > 10 ? name[..10] : name
};

static string FormatMicroseconds(double us)
{
    if (us < 1) return $"{us * 1000:F0} ns";
    if (us < 1000) return $"{us:F1} us";
    if (us < 1000000) return $"{us / 1000:F1} ms";
    return $"{us / 1000000:F2} s";
}

static string FormatThroughput(double perSec)
{
    if (perSec >= 1_000_000) return $"{perSec / 1_000_000:F1}M/s";
    if (perSec >= 1_000) return $"{perSec / 1_000:F0}K/s";
    return $"{perSec:F0}/s";
}

static async Task SaveResults(List<BenchmarkResult> results, string outputPath)
{
    var output = new
    {
        timestamp = DateTime.UtcNow.ToString("o"),
        results = results.Select(r => new
        {
            scenario = r.ScenarioId,
            scenarioName = r.ScenarioName,
            library = r.LibraryName,
            runtime = r.Runtime,
            iterations = r.Iterations,
            medianUs = r.MedianMicroseconds,
            meanUs = r.MeanMicroseconds,
            stdDevUs = r.StdDevMicroseconds,
            minUs = r.MinMicroseconds,
            maxUs = r.MaxMicroseconds,
            memoryKB = r.MemoryAllocatedKB,
            throughputPerSec = r.ThroughputPerSecond,
            validationResult = r.ValidationResult
        }),
        rankings = results
            .GroupBy(r => r.ScenarioId)
            .Select(g => new
            {
                scenario = g.Key,
                fastest = g.OrderBy(r => r.MedianMicroseconds).First().LibraryName,
                libraries = g.OrderBy(r => r.MedianMicroseconds)
                    .Select((r, idx) => new
                    {
                        rank = idx + 1,
                        library = r.LibraryName,
                        medianUs = r.MedianMicroseconds,
                        relativeSpeed = r.MedianMicroseconds / g.Min(x => x.MedianMicroseconds)
                    })
            })
    };

    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(output, jsonOptions);
    await File.WriteAllTextAsync(outputPath, json);
}
