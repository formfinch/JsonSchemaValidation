using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace JsonSchemaValidationBenchmarks.Adapters;

public sealed class JsonSchemaNetAdapter : ISchemaValidatorAdapter
{
    public string Name => "JsonSchema.Net";
    public string Runtime => "dotnet";

    private JsonSchema? _schema;
    private readonly EvaluationOptions _options;

    public JsonSchemaNetAdapter()
    {
        _options = new EvaluationOptions
        {
            OutputFormat = OutputFormat.Flag,
            RequireFormatValidation = true
        };
    }

    public void PrepareSchema(string schemaJson)
    {
        _schema = JsonSchema.FromText(schemaJson);
    }

    public bool Validate(string dataJson)
    {
        var node = JsonNode.Parse(dataJson);
        var result = _schema!.Evaluate(node, _options);
        return result.IsValid;
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
