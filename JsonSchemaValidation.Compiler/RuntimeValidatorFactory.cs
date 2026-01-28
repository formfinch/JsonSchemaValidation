// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FormFinch.JsonSchemaValidation.Compiler;

/// <summary>
/// Factory for compiling JSON schemas to validators at runtime using Roslyn.
/// </summary>
internal sealed class RuntimeValidatorFactory : IDisposable
{
    private readonly SchemaCodeGenerator _codeGenerator = new() { UseGeneratedRegex = false };
    private readonly Dictionary<string, ICompiledValidator> _cache = new(StringComparer.Ordinal);
    private readonly List<AssemblyLoadContext> _loadContexts = [];
    private readonly object _lock = new();
    private readonly ICompiledValidatorRegistry? _registry;

    private const string GeneratedNamespace = "JsonSchemaValidation.RuntimeCompiled";

    /// <summary>
    /// Creates a new RuntimeValidatorFactory without a registry.
    /// Compiled validators with external $ref will fail to initialize.
    /// </summary>
    public RuntimeValidatorFactory() : this(null, false)
    {
    }

    /// <summary>
    /// Creates a new RuntimeValidatorFactory with a registry for resolving external $ref.
    /// </summary>
    /// <param name="registry">The registry for resolving external $ref dependencies.</param>
    public RuntimeValidatorFactory(ICompiledValidatorRegistry? registry, bool forceAnnotationTracking = false)
    {
        _registry = registry;
        _codeGenerator.ForceAnnotationTracking = forceAnnotationTracking;
    }

    /// <summary>
    /// Compiles a single schema to a validator.
    /// Results are cached by schema content hash.
    /// </summary>
    public ICompiledValidator Compile(string schemaJson)
    {
        using var doc = JsonDocument.Parse(schemaJson);
        return Compile(doc.RootElement);
    }

    /// <summary>
    /// Compiles a single schema to a validator.
    /// Results are cached by schema content hash.
    /// </summary>
    public ICompiledValidator Compile(JsonElement schema)
    {
        var hash = SchemaHasher.ComputeHash(schema);

        lock (_lock)
        {
            if (_cache.TryGetValue(hash, out var cached))
            {
                return cached;
            }
        }

        // Generate and compile
        var validators = CompileSchemas([(hash, schema)]);

        lock (_lock)
        {
            if (validators.TryGetValue(hash, out var validator))
            {
                _cache[hash] = validator;
                return validator;
            }
        }

        throw new InvalidOperationException($"Failed to compile schema with hash {hash}");
    }

    /// <summary>
    /// Compiles multiple schemas into a single assembly for efficiency.
    /// Returns a dictionary mapping schema content hash to validator.
    /// </summary>
    public IReadOnlyDictionary<string, ICompiledValidator> CompileAll(IEnumerable<string> schemaJsons)
    {
        var schemas = new List<(string Hash, JsonElement Schema)>();

        foreach (var json in schemaJsons)
        {
            using var doc = JsonDocument.Parse(json);
            var hash = SchemaHasher.ComputeHash(doc.RootElement);

            lock (_lock)
            {
                if (!_cache.ContainsKey(hash))
                {
                    schemas.Add((hash, doc.RootElement.Clone()));
                }
            }
        }

        if (schemas.Count == 0)
        {
            lock (_lock)
            {
                return new Dictionary<string, ICompiledValidator>(_cache);
            }
        }

        var newValidators = CompileSchemas(schemas);

        lock (_lock)
        {
            foreach (var (hash, validator) in newValidators)
            {
                _cache[hash] = validator;
            }

            return new Dictionary<string, ICompiledValidator>(_cache);
        }
    }

    /// <summary>
    /// Compiles multiple schemas into a single assembly for efficiency.
    /// Returns a dictionary mapping schema content hash to validator.
    /// </summary>
    public IReadOnlyDictionary<string, ICompiledValidator> CompileAll(IEnumerable<JsonElement> schemas)
    {
        var schemaList = new List<(string Hash, JsonElement Schema)>();

        foreach (var schema in schemas)
        {
            var hash = SchemaHasher.ComputeHash(schema);

            lock (_lock)
            {
                if (!_cache.ContainsKey(hash))
                {
                    schemaList.Add((hash, schema.Clone()));
                }
            }
        }

        if (schemaList.Count == 0)
        {
            lock (_lock)
            {
                return new Dictionary<string, ICompiledValidator>(_cache);
            }
        }

        var newValidators = CompileSchemas(schemaList);

        lock (_lock)
        {
            foreach (var (hash, validator) in newValidators)
            {
                _cache[hash] = validator;
            }

            return new Dictionary<string, ICompiledValidator>(_cache);
        }
    }

    /// <summary>
    /// Checks if a validator for the given schema is already cached.
    /// </summary>
    public bool IsCached(string schemaJson)
    {
        using var doc = JsonDocument.Parse(schemaJson);
        var hash = SchemaHasher.ComputeHash(doc.RootElement);

        lock (_lock)
        {
            return _cache.ContainsKey(hash);
        }
    }

    /// <summary>
    /// Gets a cached validator by schema hash, or null if not cached.
    /// </summary>
    public ICompiledValidator? GetCached(string hash)
    {
        lock (_lock)
        {
            return _cache.TryGetValue(hash, out var validator) ? validator : null;
        }
    }

    /// <summary>
    /// Clears all cached validators and unloads compiled assemblies.
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _cache.Clear();

            foreach (var context in _loadContexts)
            {
                context.Unload();
            }

            _loadContexts.Clear();
        }
    }

    private Dictionary<string, ICompiledValidator> CompileSchemas(List<(string Hash, JsonElement Schema)> schemas)
    {
        var result = new Dictionary<string, ICompiledValidator>(StringComparer.Ordinal);
        var syntaxTrees = new List<SyntaxTree>();
        var classNames = new Dictionary<string, string>(StringComparer.Ordinal);

        // Generate C# code for each schema
        foreach (var (hash, schema) in schemas)
        {
            var className = $"CompiledValidator_{hash}";
            var genResult = _codeGenerator.Generate(schema, GeneratedNamespace, className);

            if (!genResult.Success)
            {
                throw new InvalidOperationException($"Code generation failed for schema {hash}: {genResult.Error}");
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(genResult.GeneratedCode!);
            syntaxTrees.Add(syntaxTree);
            classNames[hash] = className;
        }

        // Compile all schemas into one assembly
        var assembly = CompileToAssembly(syntaxTrees);

        // Create validator instances
        foreach (var (hash, className) in classNames)
        {
            var fullTypeName = $"{GeneratedNamespace}.{className}";
            var type = assembly.GetType(fullTypeName)
                ?? throw new InvalidOperationException($"Type {fullTypeName} not found in compiled assembly");

            var validator = (ICompiledValidator)(Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Failed to create instance of {fullTypeName}"));

            result[hash] = validator;
        }

        // Initialize registry-aware validators (if registry is available)
        // If no registry is provided, external refs remain unresolved and validation will fail
        // for those refs (the generated code has null checks that return false)
        if (_registry != null)
        {
            foreach (var validator in result.Values)
            {
                if (validator is IRegistryAwareCompiledValidator registryAware)
                {
                    registryAware.RegisterSubschemas(_registry);
                }
            }

            foreach (var validator in result.Values)
            {
                if (validator is IRegistryAwareCompiledValidator registryAware)
                {
                    registryAware.Initialize(_registry);
                }
            }
        }
        else
        {
            // Check if any validators require a registry but none was provided
            foreach (var validator in result.Values)
            {
                if (validator is IRegistryAwareCompiledValidator)
                {
                    throw new InvalidOperationException(
                        $"Validator {validator.GetType().Name} has external $ref dependencies but no registry was provided. " +
                        "Create RuntimeValidatorFactory with a registry to resolve external references.");
                }
            }
        }

        return result;
    }

    private Assembly CompileToAssembly(List<SyntaxTree> syntaxTrees)
    {
        var assemblyName = $"RuntimeValidators_{Guid.NewGuid():N}";

        // Get references from the current runtime
        var references = GetMetadataReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString());

            throw new InvalidOperationException(
                $"Compilation failed:\n{string.Join("\n", errors)}");
        }

        ms.Seek(0, SeekOrigin.Begin);

        // Use a collectible load context so the assembly can be unloaded
        var loadContext = new AssemblyLoadContext(assemblyName, isCollectible: true);

        lock (_lock)
        {
            _loadContexts.Add(loadContext);
        }

        return loadContext.LoadFromStream(ms);
    }

    private static List<MetadataReference> GetMetadataReferences()
    {
        var references = new List<MetadataReference>();

        // Add runtime assemblies
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        // Core runtime
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Linq.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll")));

        // System.Uri
        references.Add(MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location));

        // System.Memory (for ReadOnlySequence<T> used by JsonElement)
        references.Add(MetadataReference.CreateFromFile(typeof(System.Buffers.ReadOnlySequence<>).Assembly.Location));

        // System.Text.Json
        references.Add(MetadataReference.CreateFromFile(typeof(JsonElement).Assembly.Location));

        // System.Text.RegularExpressions
        references.Add(MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).Assembly.Location));

        // System.Runtime.Numerics (for BigInteger used in integer type validation)
        references.Add(MetadataReference.CreateFromFile(typeof(System.Numerics.BigInteger).Assembly.Location));

        // Our abstractions (ICompiledValidator)
        references.Add(MetadataReference.CreateFromFile(typeof(ICompiledValidator).Assembly.Location));

        return references;
    }

    public void Dispose()
    {
        ClearCache();
    }
}
