using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Compiler;
using JsonSchemaValidation.CompiledValidators;

namespace JsonSchemaValidationBenchmarks.Adapters;

/// <summary>
/// Adapter that compiles schemas at runtime using Roslyn.
/// Schema compilation happens in PrepareSchema (not benchmarked).
/// Validation is benchmarked and runs compiled native code.
/// </summary>
public sealed class JsonSchemaValidationCompiledAdapter : IPreparsedSchemaValidatorAdapter
{
    public string Name => "JSV-Compiled";
    public string Runtime => "dotnet";

    // Shared registry and factory with caching across all adapter instances
    private static readonly CompiledValidatorRegistry Registry = CreateRegistryWithMetaschemas();
    private static readonly RuntimeValidatorFactory Factory = new(Registry);

    private static CompiledValidatorRegistry CreateRegistryWithMetaschemas()
    {
        var registry = new CompiledValidatorRegistry();

        // Pre-register all metaschemas so they can be resolved by external $ref
        foreach (var metaschema in CompiledMetaschemas.GetAll())
        {
            try
            {
                registry.Register(metaschema);
            }
            catch
            {
                // Ignore registration errors
            }
        }

        // Load remote schemas for test suite compatibility
        LoadRemoteSchemas(registry);

        return registry;
    }

    private static void LoadRemoteSchemas(CompiledValidatorRegistry registry)
    {
        // Find test suite remotes path
        var basePath = AppContext.BaseDirectory;
        var remotesPath = FindRemotesPath(basePath);
        if (remotesPath == null) return;

        // Create a temporary factory for compiling remote schemas (use the registry we're populating)
        using var factory = new RuntimeValidatorFactory(registry);

        // Load draft2020-12 remotes
        LoadRemotesFromPath(registry, factory, Path.Combine(remotesPath, "draft2020-12"), "http://localhost:1234/draft2020-12/");

        // Load draft2019-09 remotes
        LoadRemotesFromPath(registry, factory, Path.Combine(remotesPath, "draft2019-09"), "http://localhost:1234/draft2019-09/");

        // Load root-level remotes
        LoadRemotesFromPath(registry, factory, remotesPath, "http://localhost:1234/", topLevelOnly: true);
    }

    private static string? FindRemotesPath(string basePath)
    {
        var current = basePath;
        for (int i = 0; i < 10; i++)
        {
            var remotesPath = Path.Combine(current, "submodules", "JSON-Schema-Test-Suite", "remotes");
            if (Directory.Exists(remotesPath))
                return remotesPath;

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return null;
    }

    private static void LoadRemotesFromPath(CompiledValidatorRegistry registry, RuntimeValidatorFactory factory, string path, string baseUrl, bool topLevelOnly = false)
    {
        if (!Directory.Exists(path)) return;

        var searchOption = topLevelOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
        foreach (var file in Directory.GetFiles(path, "*.json", searchOption))
        {
            try
            {
                var content = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(path, file).Replace("\\", "/");
                var schemaUri = new Uri(baseUrl + relativePath);

                // Compile the schema to a validator
                var validator = factory.Compile(content);

                // Register with the specific URI (not the $id from the schema)
                registry.RegisterForUri(schemaUri, validator);
            }
            catch
            {
                // Ignore errors loading remote schemas
            }
        }
    }

    private ICompiledValidator? _compiledValidator;

    public void PrepareSchema(string schemaJson)
    {
        // Compile the schema (cached if already compiled)
        _compiledValidator = Factory.Compile(schemaJson);

        // Register the validator in the registry so it can be used by other schemas with external $ref
        if (_compiledValidator != null)
        {
            try
            {
                Registry.Register(_compiledValidator);
            }
            catch
            {
                // Ignore registration errors (e.g., schema has no $id)
            }
        }
    }

    public bool Validate(string dataJson)
    {
        if (_compiledValidator == null)
        {
            throw new InvalidOperationException("PrepareSchema must be called before Validate.");
        }

        using var doc = JsonDocument.Parse(dataJson);
        return _compiledValidator.IsValid(doc.RootElement);
    }

    public bool Validate(JsonElement data)
    {
        if (_compiledValidator == null)
        {
            throw new InvalidOperationException("PrepareSchema must be called before Validate.");
        }

        return _compiledValidator.IsValid(data);
    }

    public void Dispose()
    {
        // Factory is shared and long-lived, don't dispose it here
    }
}
