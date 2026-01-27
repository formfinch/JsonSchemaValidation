// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using LateApexEarlySpeed.Json.Schema;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Validators;

/// <summary>
/// Wrapper for LateApexEarlySpeed.Json.Schema validation for benchmarking.
/// </summary>
public sealed class LateApexValidator
{
    private readonly JsonValidator _validator;

    public LateApexValidator(string schemaJson)
    {
        _validator = new JsonValidator(schemaJson);
    }

    public bool IsValid(string instanceJson)
    {
        var result = _validator.Validate(instanceJson);
        return result.IsValid;
    }

    /// <summary>
    /// Static parse method for cold parsing benchmarks.
    /// </summary>
    public static JsonValidator Parse(string schemaJson)
    {
        return new JsonValidator(schemaJson);
    }
}
