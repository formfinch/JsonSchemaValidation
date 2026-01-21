// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using NJsonSchema;

namespace FormFinch.JsonSchemaValidationBenchmarks.Adapters;

public sealed class NJsonSchemaAdapter : ISchemaValidatorAdapter
{
    public string Name => "NJsonSchema";
    public string Runtime => "dotnet";

    private JsonSchema? _schema;

    public void PrepareSchema(string schemaJson)
    {
        _schema = JsonSchema.FromJsonAsync(schemaJson).GetAwaiter().GetResult();
    }

    public bool Validate(string dataJson)
    {
        var errors = _schema!.Validate(dataJson);
        return errors.Count == 0;
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
