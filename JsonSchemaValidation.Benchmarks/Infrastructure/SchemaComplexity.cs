// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

namespace FormFinch.JsonSchemaValidation.Benchmarks.Infrastructure;

/// <summary>
/// Defines the complexity levels of benchmark test schemas.
/// </summary>
public enum SchemaComplexity
{
    /// <summary>
    /// Simple schema with single type constraint (e.g., type: string).
    /// </summary>
    Simple,

    /// <summary>
    /// Medium complexity with properties, required, and pattern.
    /// </summary>
    Medium,

    /// <summary>
    /// Complex schema with allOf/anyOf/oneOf and $ref.
    /// </summary>
    Complex,

    /// <summary>
    /// Production-grade schema (GitHub Workflow).
    /// </summary>
    Production
}
