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
    private static readonly CompiledValidatorRegistry Registry = new();
    private static readonly RuntimeValidatorFactory Factory = new(Registry);

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
