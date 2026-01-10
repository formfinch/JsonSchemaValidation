namespace JsonSchemaValidationBenchmarks.Adapters;

public interface ISchemaValidatorAdapter : IDisposable
{
    string Name { get; }

    string Runtime { get; }

    void PrepareSchema(string schemaJson);

    bool Validate(string dataJson);
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
