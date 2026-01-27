// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Json.Schema;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Validators;

/// <summary>
/// Wrapper for JsonSchema.Net validation for benchmarking.
/// </summary>
public sealed class JsonSchemaNetValidator
{
    private readonly JsonSchema _schema;
    private readonly EvaluationOptions _options;

    public JsonSchemaNetValidator(string schemaJson)
    {
        _schema = JsonSchema.FromText(schemaJson);
        _options = new EvaluationOptions
        {
            OutputFormat = OutputFormat.Flag,
            RequireFormatValidation = false // Default to annotation-only per JSON Schema spec
        };
    }

    public bool IsValid(string instanceJson)
    {
        var node = JsonNode.Parse(instanceJson);
        var result = _schema.Evaluate(node, _options);
        return result.IsValid;
    }

    public bool IsValid(JsonNode? instance)
    {
        var result = _schema.Evaluate(instance, _options);
        return result.IsValid;
    }

    /// <summary>
    /// Static parse method for cold parsing benchmarks.
    /// </summary>
    public static JsonSchema Parse(string schemaJson)
    {
        return JsonSchema.FromText(schemaJson);
    }
}
