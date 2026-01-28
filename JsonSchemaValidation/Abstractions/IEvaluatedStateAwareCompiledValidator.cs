// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Abstractions;

/// <summary>
/// Exposes evaluated annotations from compiled validators so callers can merge them.
/// </summary>
public interface IEvaluatedStateAwareCompiledValidator
{
    /// <summary>
    /// Returns a snapshot of evaluated properties/items collected during the last validation run.
    /// </summary>
    EvaluatedStateSnapshot GetEvaluatedStateSnapshot();
}
