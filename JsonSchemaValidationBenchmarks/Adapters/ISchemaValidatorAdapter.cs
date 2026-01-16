using System.Text.Json;

namespace JsonSchemaValidationBenchmarks.Adapters;

public interface ISchemaValidatorAdapter : IDisposable
{
    string Name { get; }

    string Runtime { get; }

    void PrepareSchema(string schemaJson);

    bool Validate(string dataJson);
}

/// <summary>
/// Adapter that supports validation of pre-parsed JSON data.
/// This enables fair benchmarking by excluding JSON parsing time.
/// </summary>
public interface IPreparsedSchemaValidatorAdapter : ISchemaValidatorAdapter
{
    bool Validate(JsonElement data);
}

public interface IAsyncSchemaValidatorAdapter : ISchemaValidatorAdapter
{
    Task PrepareSchemaAsync(string schemaJson);

    Task<bool> ValidateAsync(string dataJson);

    Task<IReadOnlyList<double>> RunBenchmarkAsync(string dataJson, int iterations);

    /// <summary>
    /// Runs benchmark measuring full end-to-end time (schema compilation + validation) per iteration.
    /// </summary>
    Task<IReadOnlyList<double>> RunBenchmarkWithCompilationAsync(string schemaJson, string dataJson, int iterations);
}
