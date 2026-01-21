using LateApexEarlySpeed.Json.Schema;

namespace FormFinch.JsonSchemaValidationBenchmarks.Adapters;

public sealed class LateApexAdapter : ISchemaValidatorAdapter
{
    public string Name => "LateApex";
    public string Runtime => "dotnet";

    private JsonValidator? _validator;

    public void PrepareSchema(string schemaJson)
    {
        _validator = new JsonValidator(schemaJson);
    }

    public bool Validate(string dataJson)
    {
        if (_validator is null)
        {
            throw new InvalidOperationException("Schema not prepared");
        }

        var result = _validator.Validate(dataJson);
        return result.IsValid;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
