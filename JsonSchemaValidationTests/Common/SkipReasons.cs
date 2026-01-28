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
    /// Compiled validators cannot perform runtime dynamic scope resolution for $dynamicRef.
    /// $dynamicRef requires runtime stack inspection to find the first matching $dynamicAnchor
    /// in the dynamic call chain. Compiled validators resolve references statically at compile time.
    /// </summary>
    public const string DynamicScopeResolution =
        "Compiled validators cannot perform runtime dynamic scope resolution for $dynamicRef";

    /// <summary>
    /// Compiled validators cannot perform runtime recursive scope resolution for $recursiveRef.
    /// Similar to $dynamicRef, $recursiveRef requires runtime stack inspection to find
    /// the innermost schema with $recursiveAnchor.
    /// </summary>
    public const string RecursiveRefResolution =
        "Compiled validators cannot perform runtime recursive scope resolution for $recursiveRef";

    /// <summary>
    /// Cannot compile remote subschemas that contain $ref to sibling definitions.
    /// Subschemas extracted from remote files that contain $ref to sibling definitions
    /// can't be compiled standalone because the internal references can't be resolved
    /// without the full document context.
    /// </summary>
    public const string RemoteRefWithInternalRef =
        "Cannot compile remote subschemas that contain $ref to sibling definitions";

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
    /// Base URI changes in subschemas require full document context.
    /// When a subschema changes the base URI, compiled validators cannot always
    /// resolve references correctly without the full document context.
    /// </summary>
    public const string BaseUriChange =
        "Base URI changes in subschemas require full document context";

    /// <summary>
    /// Anchor resolution requires full document context.
    /// Some anchor resolution scenarios require access to the full document
    /// to correctly resolve the anchor reference.
    /// </summary>
    public const string AnchorResolution =
        "Anchor resolution requires full document context";

    /// <summary>
    /// RuntimeValidatorFactory does not support additionalItems keyword.
    /// The additionalItems keyword (used in Drafts 3-7 and 2019-09 for tuple validation)
    /// is not implemented in the runtime compiler.
    /// </summary>
    public const string AdditionalItemsNotSupported =
        "RuntimeValidatorFactory does not support additionalItems keyword";

    /// <summary>
    /// RuntimeValidatorFactory does not support Draft 3 specific keywords.
    /// Draft 3 uses unique keywords like divisibleBy, disallow, and extends
    /// that are not implemented in the runtime compiler.
    /// </summary>
    public const string Draft3KeywordsNotSupported =
        "RuntimeValidatorFactory does not support Draft 3 specific keywords (divisibleBy, disallow, extends)";

    /// <summary>
    /// RuntimeValidatorFactory does not support dependencies keyword.
    /// The dependencies keyword (used in Drafts 3-7) is not implemented
    /// in the runtime compiler.
    /// </summary>
    public const string DependenciesNotSupported =
        "RuntimeValidatorFactory does not support dependencies keyword";

    /// <summary>
    /// RuntimeValidatorFactory does not support Draft 3 required property format.
    /// Draft 3 uses required as a boolean on property definitions rather than
    /// an array at the schema level.
    /// </summary>
    public const string Draft3RequiredNotSupported =
        "RuntimeValidatorFactory does not support Draft 3 required property format";

    /// <summary>
    /// RuntimeValidatorFactory does not support items as array (tuple validation).
    /// When items is an array of schemas, each schema validates the corresponding
    /// array element, requiring additionalItems for remaining elements.
    /// </summary>
    public const string ItemsAsArrayNotSupported =
        "RuntimeValidatorFactory does not support items as array (tuple validation)";

    /// <summary>
    /// RuntimeValidatorFactory does not support Draft 3 type with schema arrays.
    /// Draft 3 allows type to be an array containing schemas for union types.
    /// </summary>
    public const string Draft3TypeSchemasNotSupported =
        "RuntimeValidatorFactory does not support Draft 3 type arrays with schemas";

    /// <summary>
    /// RuntimeValidatorFactory does not support Draft 3 format names.
    /// Draft 3 uses different format names like 'color', 'host-name', 'ip-address'
    /// instead of the modern names.
    /// </summary>
    public const string Draft3FormatNamesNotSupported =
        "RuntimeValidatorFactory does not support Draft 3 format names (color, host-name, ip-address)";

    /// <summary>
    /// RuntimeValidatorFactory does not support id resolution in older drafts.
    /// Older drafts use 'id' instead of '$id' with different resolution rules.
    /// </summary>
    public const string IdResolutionNotSupported =
        "RuntimeValidatorFactory does not support id resolution in older drafts";

    /// <summary>
    /// RuntimeValidatorFactory does not support metaschema validation.
    /// Validating against the metaschema requires all vocabulary validators to be available.
    /// </summary>
    public const string MetaschemaValidationNotSupported =
        "RuntimeValidatorFactory does not support metaschema validation for this draft";

    /// <summary>
    /// Compiled validators don't have cycle detection and will stack overflow on recursive schemas.
    /// The infinite-loop-detection tests specifically check that validators can handle recursive
    /// schemas without crashing, but compiled validators resolve refs statically without tracking visited nodes.
    /// </summary>
    public const string InfiniteLoopNotSupported =
        "Compiled validators don't have cycle detection for recursive schemas";

    /// <summary>
    /// RuntimeValidatorFactory does not support contentMediaType/contentEncoding validation.
    /// Content validation (base64, JSON) is not implemented in the runtime compiler.
    /// </summary>
    public const string ContentValidationNotSupported =
        "RuntimeValidatorFactory does not support contentMediaType/contentEncoding validation";

    /// <summary>
    /// RuntimeValidatorFactory does not fully support unevaluatedItems/unevaluatedProperties.
    /// These keywords require annotation tracking across applicators to determine which items/properties
    /// have been evaluated, which is complex for compiled validators.
    /// </summary>
    public const string UnevaluatedNotSupported =
        "RuntimeValidatorFactory does not fully support unevaluatedItems/unevaluatedProperties with applicators";

    /// <summary>
    /// Complex $dynamicRef scenarios involving external schemas or multiple dynamic paths.
    /// While basic $dynamicRef is supported, complex scenarios that require scope propagation
    /// across schema boundaries (external $ref to schemas with $dynamicRef/$dynamicAnchor)
    /// or multiple dynamic paths through different $ref chains are not fully supported.
    /// </summary>
    public const string ComplexDynamicRefNotSupported =
        "Complex $dynamicRef with external schemas or multiple dynamic paths not fully supported";
}
