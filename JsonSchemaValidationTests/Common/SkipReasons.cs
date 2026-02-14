// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

namespace FormFinch.JsonSchemaValidationTests.Common;

/// <summary>
/// Standardized skip reasons for compiled validator tests.
/// These reasons document why certain tests cannot pass with compiled validators
/// due to fundamental architectural limitations.
/// </summary>
internal static class SkipReasons
{
    /// <summary>
    /// Compiled validators cannot enable/disable keywords based on $vocabulary.
    /// Vocabulary-based validation requires checking $vocabulary in metaschema to
    /// enable/disable keyword processing at runtime.
    /// </summary>
    public const string VocabularyBased =
        "Compiled validators cannot enable/disable keywords based on $vocabulary";

    /// <summary>
    /// Compiled validators cannot process $ref targets according to their declared $schema.
    /// Cross-draft compatibility requires processing $ref targets according to their
    /// declared $schema, which requires runtime schema detection.
    /// </summary>
    public const string CrossDraft =
        "Compiled validators cannot process $ref targets according to their declared $schema";

    /// <summary>
    /// Complex $dynamicRef scenarios involving external schemas or multiple dynamic paths.
    /// While basic $dynamicRef is supported, complex scenarios that require scope propagation
    /// across schema boundaries (external $ref to schemas with $dynamicRef/$dynamicAnchor)
    /// or multiple dynamic paths through different $ref chains are not fully supported.
    /// </summary>
    public const string ComplexDynamicRefNotSupported =
        "Complex $dynamicRef with external schemas or multiple dynamic paths not fully supported";
}
