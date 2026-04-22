// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;

/// <summary>
/// Orchestrates JavaScript (ESM) validator emission for a JSON Schema.
/// Reuses the language-agnostic schema analysis from JsonSchemaValidation.CodeGeneration
/// and delegates per-keyword emission to IJsKeywordCodeGenerator implementations.
/// </summary>
public sealed class JsSchemaCodeGenerator
{
    private sealed record FragmentValidatorInfo(string Hash, string Uri);
    private sealed record MetaSchemaBehavior(
        bool ValidationVocabularyEnabled,
        bool MetaSchemaDeclaresFormatAssertionVocabulary);

    private readonly List<IJsKeywordCodeGenerator> _keywordGenerators;
    private readonly SubschemaExtractor _extractor = new();

    /// <summary>
    /// The default draft version to use when a schema doesn't have an explicit $schema keyword.
    /// If null, defaults to Draft 2020-12.
    /// </summary>
    public SchemaDraft? DefaultDraft { get; set; }

    /// <summary>
    /// Whether to assert supported "format" values for Draft 2020-12. Earlier
    /// supported drafts still assert format by default. Defaults to false for
    /// spec-conformant Draft 2020-12 output; tests and consumers can opt in.
    /// </summary>
    public bool FormatAssertionEnabled { get; set; }

    /// <summary>
    /// Forces property/item annotation tracking even when the schema itself does
    /// not contain unevaluated* keywords. Useful for registry-preloaded validators
    /// that may be referenced by a caller that does track unevaluated annotations.
    /// </summary>
    public bool AlwaysTrackAnnotations { get; set; }

    /// <summary>
    /// The import specifier used for the shared JS runtime module.
    /// Defaults to a relative ESM import sibling to the emitted validator.
    /// </summary>
    public string RuntimeImportSpecifier { get; set; } = "./jsv-runtime.js";

    /// <summary>
    /// Optional preloaded schemas keyed by absolute URI. Used for metaschema-aware
    /// generation decisions such as $vocabulary handling.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ExternalSchemaDocuments { get; set; }

    public JsSchemaCodeGenerator()
    {
        _keywordGenerators =
        [
            new JsRefCodeGenerator(),
            new JsDynamicRefCodeGenerator(),
            new JsTypeCodeGenerator(),
            new JsRequiredCodeGenerator(),
            new JsEnumCodeGenerator(),
            new JsConstCodeGenerator(),
            new JsPropertyNamesCodeGenerator(),
            new JsDependentRequiredCodeGenerator(),
            new JsDependentSchemasCodeGenerator(),
            new JsDependenciesCodeGenerator(),
            new JsNumericConstraintsCodeGenerator(),
            new JsStringConstraintsCodeGenerator(),
            new JsArrayConstraintsCodeGenerator(),
            new JsObjectConstraintsCodeGenerator(),
            new JsPatternCodeGenerator(),
            new JsPrefixItemsCodeGenerator(),
            new JsAdditionalItemsCodeGenerator(),
            new JsItemsCodeGenerator(),
            new JsContainsCodeGenerator(),
            new JsPropertiesCodeGenerator(),
            new JsPatternPropertiesCodeGenerator(),
            new JsAdditionalPropertiesCodeGenerator(),
            new JsAllOfCodeGenerator(),
            new JsAnyOfCodeGenerator(),
            new JsOneOfCodeGenerator(),
            new JsNotCodeGenerator(),
            new JsIfThenElseCodeGenerator(),
            new JsFormatCodeGenerator(),
            new JsUnevaluatedPropertiesCodeGenerator(),
            new JsUnevaluatedItemsCodeGenerator(),
        ];
        _keywordGenerators.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// Generates an ESM JavaScript validator module from a schema file.
    /// </summary>
    public GenerationResult Generate(string schemaPath)
    {
        try
        {
            var json = File.ReadAllText(schemaPath);
            using var doc = JsonDocument.Parse(json);
            return Generate(doc.RootElement, schemaPath);
        }
        catch (Exception ex)
        {
            return GenerationResult.Failed($"Failed to read schema: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates an ESM JavaScript validator module from a schema element.
    /// </summary>
    public GenerationResult Generate(JsonElement schema, string? sourcePath = null)
    {
        try
        {
            var draftResult = SchemaDraftDetector.DetectDraft(schema, DefaultDraft);
            if (!draftResult.Success)
            {
                return GenerationResult.Failed(draftResult.ErrorMessage!);
            }
            var detectedDraft = draftResult.Draft;

            var gateRejection = JsCapabilityGate.CheckSupported(schema, detectedDraft);
            if (gateRejection != null)
            {
                return GenerationResult.Failed(gateRejection);
            }

            var schemaUri = ExtractSchemaUri(schema);
            Uri? baseUri = null;
            if (!string.IsNullOrEmpty(schemaUri))
            {
                Uri.TryCreate(schemaUri, UriKind.Absolute, out baseUri);
            }

            var uniqueSchemas = _extractor.ExtractUniqueSubschemas(schema, baseUri, DefaultDraft);
            var rootHash = SchemaHasher.ComputeHash(schema);
            var metaSchemaBehavior = DetermineMetaSchemaBehavior(schema, detectedDraft);
            var effectiveFormatAssertionEnabled = detectedDraft == SchemaDraft.Draft202012 &&
                                                 (FormatAssertionEnabled ||
                                                  metaSchemaBehavior.MetaSchemaDeclaresFormatAssertionVocabulary);
            var requiresPropertyAnnotations = AlwaysTrackAnnotations || _extractor.HasUnevaluatedProperties;
            var requiresItemAnnotations = AlwaysTrackAnnotations || _extractor.HasUnevaluatedItems;
            var rootBaseUri = uniqueSchemas.Values.FirstOrDefault(i => i.JsonPointerPath == string.Empty)?.EffectiveBaseUri;
            var requiresScopeTracking = detectedDraft == SchemaDraft.Draft202012 &&
                uniqueSchemas.Values.Any(s =>
                    (s.Schema.ValueKind == JsonValueKind.Object && s.Schema.TryGetProperty("$dynamicRef", out _)) ||
                    s.DynamicAnchors.Count > 0 ||
                    s.ResourceAnchors.Count > 0);

            // Reachability pass: filter out subschemas reached only through
            // annotation-only keywords (contentSchema, default, examples, ...) so
            // we don't emit validators for them — those would run JS emitters on
            // metadata and can legitimately fail (e.g., items-as-array in 2020-12
            // inside contentSchema). Also detects ambiguous-resource ref collapse
            // and rejects pre-emission.
            var reach = JsSchemaReachability.Analyze(schema, detectedDraft, uniqueSchemas);
            if (reach.Rejection != null)
            {
                return GenerationResult.Failed(reach.Rejection);
            }
            var fragmentValidators = CollectFragmentValidators(uniqueSchemas, rootBaseUri, reach.ReachableHashes);
            var requiresRegistryLookup = uniqueSchemas
                .Where(kvp => reach.ReachableHashes.Contains(kvp.Key))
                .Any(kvp => ContainsExternalRef(kvp.Value.Schema));
            var requiresRegistryParameter = requiresRegistryLookup || requiresScopeTracking;

            var methods = new StringBuilder();
            var runtimeImports = new SortedSet<string>(StringComparer.Ordinal);
            if (requiresPropertyAnnotations || requiresItemAnnotations)
            {
                runtimeImports.Add("EvaluatedState");
                runtimeImports.Add("escapeJsonPointer");
            }
            if (requiresScopeTracking)
            {
                runtimeImports.Add("CompiledValidatorScope");
            }
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                if (!reach.ReachableHashes.Contains(hash)) continue;
                methods.AppendLine(GenerateValidationFunction(
                    subschemaInfo,
                    uniqueSchemas,
                    detectedDraft,
                    metaSchemaBehavior.ValidationVocabularyEnabled,
                    effectiveFormatAssertionEnabled,
                    requiresPropertyAnnotations,
                    requiresItemAnnotations,
                    rootBaseUri,
                    requiresScopeTracking,
                    requiresRegistryParameter,
                    runtimeImports));
                methods.AppendLine();
            }

            var module = GenerateModule(
                schemaUri,
                rootHash,
                methods.ToString(),
                runtimeImports,
                requiresPropertyAnnotations || requiresItemAnnotations,
                requiresScopeTracking,
                requiresRegistryParameter,
                fragmentValidators);
            var fileName = DeriveFileName(schemaUri, sourcePath);
            return GenerationResult.Succeeded(module, fileName);
        }
        catch (Exception ex)
        {
            return GenerationResult.Failed($"Code generation failed: {ex.Message}");
        }
    }

    private string GenerateValidationFunction(
        SubschemaInfo subschemaInfo,
        Dictionary<string, SubschemaInfo> allSchemas,
        SchemaDraft detectedDraft,
        bool validationVocabularyEnabled,
        bool effectiveFormatAssertionEnabled,
        bool requiresPropertyAnnotations,
        bool requiresItemAnnotations,
        Uri? rootBaseUri,
        bool requiresScopeTracking,
        bool requiresRegistryParameter,
        SortedSet<string> runtimeImports)
    {
        var context = CreateContext(
            subschemaInfo,
            allSchemas,
            detectedDraft,
            validationVocabularyEnabled,
            effectiveFormatAssertionEnabled,
            requiresPropertyAnnotations,
            requiresItemAnnotations,
            rootBaseUri,
            requiresScopeTracking,
            requiresRegistryParameter);
        var sb = new StringBuilder();

        var parameterParts = new List<string> { "v" };
        if (requiresScopeTracking)
        {
            parameterParts.Add("_scope");
        }
        if (requiresPropertyAnnotations || requiresItemAnnotations)
        {
            parameterParts.Add("_eval");
        }
        if (requiresScopeTracking || requiresPropertyAnnotations || requiresItemAnnotations)
        {
            parameterParts.Add("_loc");
        }
        if (requiresRegistryParameter)
        {
            parameterParts.Add("_registry");
        }
        var parameters = string.Join(", ", parameterParts);
        sb.AppendLine($"function validate_{subschemaInfo.Hash}({parameters}) {{");

        if (subschemaInfo.Schema.ValueKind == JsonValueKind.True)
        {
            sb.AppendLine("  return true;");
            sb.AppendLine("}");
            return sb.ToString();
        }

        if (requiresScopeTracking)
        {
            var scopeEntry = BuildScopePushCode(subschemaInfo, requiresPropertyAnnotations || requiresItemAnnotations, requiresRegistryParameter);
            if (!string.IsNullOrWhiteSpace(scopeEntry))
            {
                foreach (var line in scopeEntry.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine($"  {line.TrimEnd()}");
                    }
                }
            }
        }
        if (subschemaInfo.Schema.ValueKind == JsonValueKind.False)
        {
            sb.AppendLine("  return false;");
            sb.AppendLine("}");
            return sb.ToString();
        }

        // Draft 7 and earlier: $ref overrides all sibling keywords — but only
        // when $ref is a USABLE string. A $ref: "" or $ref: {} would otherwise
        // mask every sibling without JsRefCodeGenerator emitting anything,
        // compiling to an always-true validator.
        var refMasksSiblings = detectedDraft <= SchemaDraft.Draft7
            && subschemaInfo.Schema.ValueKind == JsonValueKind.Object
            && subschemaInfo.Schema.TryGetProperty("$ref", out var maskingRef)
            && maskingRef.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(maskingRef.GetString());

        foreach (var generator in _keywordGenerators)
        {
            if (refMasksSiblings && generator.Keyword != "$ref")
            {
                continue;
            }

            if (!generator.CanGenerate(subschemaInfo.Schema))
            {
                continue;
            }

            foreach (var import in generator.GetRuntimeImports(context))
            {
                runtimeImports.Add(import);
            }

            var code = generator.GenerateCode(context);
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            foreach (var line in code.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine($"  {line.TrimEnd()}");
                }
            }
        }

        sb.AppendLine("  return true;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private JsCodeGenerationContext CreateContext(
        SubschemaInfo subschemaInfo,
        Dictionary<string, SubschemaInfo> allSchemas,
        SchemaDraft detectedDraft,
        bool validationVocabularyEnabled,
        bool effectiveFormatAssertionEnabled,
        bool requiresPropertyAnnotations,
        bool requiresItemAnnotations,
        Uri? rootBaseUri,
        bool requiresScopeTracking,
        bool requiresRegistryParameter)
    {
        var effectiveBaseUri = subschemaInfo.EffectiveBaseUri ?? rootBaseUri;
        return new JsCodeGenerationContext
        {
            CurrentSchema = subschemaInfo.Schema,
            CurrentHash = subschemaInfo.Hash,
            CurrentResourceRootHash = subschemaInfo.ResourceRootHash,
            GetSubschemaHash = element => SchemaHasher.ComputeHash(element),
            ResolveLocalRef = refValue => _extractor.ResolveLocalRef(refValue),
            ResolveInternalId = uri => _extractor.ResolveInternalId(uri),
            ResolveLocalRefInResource = (refValue, resourceRoot) =>
                _extractor.ResolveLocalRefInResource(refValue, resourceRoot),
            ResourceRoot = subschemaInfo.ResourceRoot,
            BaseUri = effectiveBaseUri,
            RootBaseUri = rootBaseUri,
            GetSubschemaInfo = hash => allSchemas.TryGetValue(hash, out var info) ? info : null,
            DetectedDraft = detectedDraft,
            RequiresPropertyAnnotations = requiresPropertyAnnotations,
            RequiresItemAnnotations = requiresItemAnnotations,
            FormatAssertionEnabled = effectiveFormatAssertionEnabled,
            ValidationVocabularyEnabled = validationVocabularyEnabled,
            RequiresScopeTracking = requiresScopeTracking,
            RequiresRegistry = requiresRegistryParameter
        };
    }

    private MetaSchemaBehavior DetermineMetaSchemaBehavior(JsonElement schema, SchemaDraft detectedDraft)
    {
        if ((detectedDraft != SchemaDraft.Draft201909 && detectedDraft != SchemaDraft.Draft202012) ||
            schema.ValueKind != JsonValueKind.Object ||
            !schema.TryGetProperty("$schema", out var schemaProperty) ||
            schemaProperty.ValueKind != JsonValueKind.String)
        {
            return new MetaSchemaBehavior(
                ValidationVocabularyEnabled: true,
                MetaSchemaDeclaresFormatAssertionVocabulary: false);
        }

        var schemaUri = schemaProperty.GetString();
        if (string.IsNullOrEmpty(schemaUri) ||
            ExternalSchemaDocuments == null ||
            !ExternalSchemaDocuments.TryGetValue(schemaUri, out var metaSchemaJson))
        {
            return new MetaSchemaBehavior(
                ValidationVocabularyEnabled: true,
                MetaSchemaDeclaresFormatAssertionVocabulary: false);
        }

        try
        {
            using var metaSchemaDoc = JsonDocument.Parse(metaSchemaJson);
            var metaSchemaRoot = metaSchemaDoc.RootElement;
            if (metaSchemaRoot.ValueKind != JsonValueKind.Object ||
                !metaSchemaRoot.TryGetProperty("$vocabulary", out var vocabularyElement) ||
                vocabularyElement.ValueKind != JsonValueKind.Object)
            {
                return new MetaSchemaBehavior(
                    ValidationVocabularyEnabled: true,
                    MetaSchemaDeclaresFormatAssertionVocabulary: false);
            }

            var validationVocabularyUri = detectedDraft == SchemaDraft.Draft201909
                ? "https://json-schema.org/draft/2019-09/vocab/validation"
                : "https://json-schema.org/draft/2020-12/vocab/validation";
            var formatAssertionVocabularyUri = detectedDraft == SchemaDraft.Draft201909
                ? "https://json-schema.org/draft/2019-09/vocab/format"
                : "https://json-schema.org/draft/2020-12/vocab/format-assertion";

            var validationVocabularyEnabled =
                vocabularyElement.TryGetProperty(validationVocabularyUri, out var validationElement) &&
                validationElement.ValueKind == JsonValueKind.True;
            var declaresFormatAssertionVocabulary =
                vocabularyElement.TryGetProperty(formatAssertionVocabularyUri, out _);

            return new MetaSchemaBehavior(
                ValidationVocabularyEnabled: validationVocabularyEnabled,
                MetaSchemaDeclaresFormatAssertionVocabulary: declaresFormatAssertionVocabulary);
        }
        catch
        {
            return new MetaSchemaBehavior(
                ValidationVocabularyEnabled: true,
                MetaSchemaDeclaresFormatAssertionVocabulary: false);
        }
    }

    private string GenerateModule(
        string? schemaUri,
        string rootHash,
        string methods,
        SortedSet<string> runtimeImports,
        bool requiresAnnotationTracking,
        bool requiresScopeTracking,
        bool requiresRegistry,
        IReadOnlyList<FragmentValidatorInfo> fragmentValidators)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// @ts-check");
        sb.AppendLine("// This code was generated by jsv-codegen (JS target).");
        sb.AppendLine("// Do not edit this file manually.");
        sb.AppendLine();

        if (runtimeImports.Count > 0)
        {
            sb.Append("import { ");
            sb.Append(string.Join(", ", runtimeImports));
            sb.AppendLine($" }} from {JsLiteral.String(RuntimeImportSpecifier)};");
            sb.AppendLine();
        }

        sb.Append(methods);
        sb.AppendLine();

        sb.AppendLine("/**");
        sb.AppendLine(" * Validates a JSON value against the compiled schema.");
        sb.AppendLine(" * @param {unknown} data");
        sb.AppendLine(" * @returns {boolean}");
        sb.AppendLine(" */");
        var exportParameters = requiresRegistry ? "data, registry = null" : "data";
        if (requiresScopeTracking && requiresAnnotationTracking)
        {
            if (requiresRegistry)
            {
                sb.AppendLine($"export function validateWithScope(data, scope, location = \"\", registry = null) {{ return validate_{rootHash}(data, scope, new EvaluatedState(), location, registry); }}");
                sb.AppendLine($"export function validateWithState(data, scope, evaluatedState, location = \"\", registry = null) {{ return validate_{rootHash}(data, scope, evaluatedState, location, registry); }}");
                sb.AppendLine($"export function validate({exportParameters}) {{ return validateWithState(data, CompiledValidatorScope.empty, new EvaluatedState(), \"\", registry); }}");
            }
            else
            {
                sb.AppendLine($"export function validateWithScope(data, scope, location = \"\") {{ return validate_{rootHash}(data, scope, new EvaluatedState(), location); }}");
                sb.AppendLine($"export function validateWithState(data, scope, evaluatedState, location = \"\") {{ return validate_{rootHash}(data, scope, evaluatedState, location); }}");
                sb.AppendLine($"export function validate({exportParameters}) {{ return validateWithState(data, CompiledValidatorScope.empty, new EvaluatedState(), \"\"); }}");
            }
        }
        else if (requiresScopeTracking)
        {
            if (requiresRegistry)
            {
                sb.AppendLine($"export function validateWithScope(data, scope, location = \"\", registry = null) {{ return validate_{rootHash}(data, scope, location, registry); }}");
                sb.AppendLine($"export function validate({exportParameters}) {{ return validateWithScope(data, CompiledValidatorScope.empty, \"\", registry); }}");
            }
            else
            {
                sb.AppendLine($"export function validateWithScope(data, scope, location = \"\") {{ return validate_{rootHash}(data, scope, location); }}");
                sb.AppendLine($"export function validate({exportParameters}) {{ return validateWithScope(data, CompiledValidatorScope.empty, \"\"); }}");
            }
        }
        else if (requiresAnnotationTracking)
        {
            if (requiresRegistry)
            {
                sb.AppendLine($"export function validateWithState(data, evaluatedState, location = \"\", registry = null) {{ return validate_{rootHash}(data, evaluatedState, location, registry); }}");
                sb.AppendLine($"export function validate({exportParameters}) {{ return validateWithState(data, new EvaluatedState(), \"\", registry); }}");
            }
            else
            {
                sb.AppendLine($"export function validateWithState(data, evaluatedState, location = \"\") {{ return validate_{rootHash}(data, evaluatedState, location); }}");
                sb.AppendLine($"export function validate({exportParameters}) {{ return validateWithState(data, new EvaluatedState(), \"\"); }}");
            }
        }
        else if (requiresRegistry)
        {
            sb.AppendLine($"export function validate({exportParameters}) {{ return validate_{rootHash}(data, registry); }}");
        }
        else
        {
            sb.AppendLine($"export function validate({exportParameters}) {{ return validate_{rootHash}(data); }}");
        }
        sb.AppendLine();

        var schemaUriLiteral = string.IsNullOrEmpty(schemaUri)
            ? "null"
            : JsLiteral.String(schemaUri);
        sb.AppendLine($"export const schemaUri = {schemaUriLiteral};");
        sb.AppendLine();

        sb.AppendLine("export const fragmentValidators = {");
        foreach (var fragment in fragmentValidators)
        {
            sb.AppendLine($"  {JsLiteral.String(fragment.Uri)}: {{");
            if (requiresScopeTracking && requiresAnnotationTracking && requiresRegistry)
            {
                sb.AppendLine($"    validate(data, registry = null) {{ return validate_{fragment.Hash}(data, CompiledValidatorScope.empty, new EvaluatedState(), \"\", registry); }},");
                sb.AppendLine($"    validateWithScope(data, scope, location = \"\", registry = null) {{ return validate_{fragment.Hash}(data, scope, new EvaluatedState(), location, registry); }},");
                sb.AppendLine($"    validateWithState(data, scope, evaluatedState, location = \"\", registry = null) {{ return validate_{fragment.Hash}(data, scope, evaluatedState, location, registry); }}");
            }
            else if (requiresScopeTracking && requiresAnnotationTracking)
            {
                sb.AppendLine($"    validate(data) {{ return validate_{fragment.Hash}(data, CompiledValidatorScope.empty, new EvaluatedState(), \"\"); }},");
                sb.AppendLine($"    validateWithScope(data, scope, location = \"\") {{ return validate_{fragment.Hash}(data, scope, new EvaluatedState(), location); }},");
                sb.AppendLine($"    validateWithState(data, scope, evaluatedState, location = \"\") {{ return validate_{fragment.Hash}(data, scope, evaluatedState, location); }}");
            }
            else if (requiresScopeTracking && requiresRegistry)
            {
                sb.AppendLine($"    validate(data, registry = null) {{ return validate_{fragment.Hash}(data, CompiledValidatorScope.empty, \"\", registry); }},");
                sb.AppendLine($"    validateWithScope(data, scope, location = \"\", registry = null) {{ return validate_{fragment.Hash}(data, scope, location, registry); }}");
            }
            else if (requiresScopeTracking)
            {
                sb.AppendLine($"    validate(data) {{ return validate_{fragment.Hash}(data, CompiledValidatorScope.empty, \"\"); }},");
                sb.AppendLine($"    validateWithScope(data, scope, location = \"\") {{ return validate_{fragment.Hash}(data, scope, location); }}");
            }
            else if (requiresAnnotationTracking && requiresRegistry)
            {
                sb.AppendLine($"    validate(data, registry = null) {{ return validate_{fragment.Hash}(data, new EvaluatedState(), \"\", registry); }},");
                sb.AppendLine($"    validateWithState(data, evaluatedState, location = \"\", registry = null) {{ return validate_{fragment.Hash}(data, evaluatedState, location, registry); }}");
            }
            else if (requiresAnnotationTracking)
            {
                sb.AppendLine($"    validate(data) {{ return validate_{fragment.Hash}(data, new EvaluatedState(), \"\"); }},");
                sb.AppendLine($"    validateWithState(data, evaluatedState, location = \"\") {{ return validate_{fragment.Hash}(data, evaluatedState, location); }}");
            }
            else if (requiresRegistry)
            {
                sb.AppendLine($"    validate(data, registry = null) {{ return validate_{fragment.Hash}(data, registry); }}");
            }
            else
            {
                sb.AppendLine($"    validate(data) {{ return validate_{fragment.Hash}(data); }}");
            }
            sb.AppendLine("  },");
        }
        sb.AppendLine("};");
        sb.AppendLine();

        sb.AppendLine(requiresScopeTracking && requiresAnnotationTracking
            ? "export default { validate, validateWithScope, validateWithState, schemaUri };"
            : requiresAnnotationTracking
                ? "export default { validate, validateWithState, schemaUri };"
                : requiresScopeTracking
                    ? "export default { validate, validateWithScope, schemaUri };"
                    : "export default { validate, schemaUri };");
        return sb.ToString();
    }

    private static string BuildScopePushCode(
        SubschemaInfo subschemaInfo,
        bool requiresAnnotationTracking,
        bool requiresRegistry)
    {
        var anchorsToInclude = subschemaInfo.IsResourceRoot
            ? subschemaInfo.ResourceAnchors
            : subschemaInfo.DynamicAnchors.Select(name => (name, subschemaInfo.Hash)).ToList();
        if (anchorsToInclude.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("_scope = _scope.push({");
        sb.AppendLine("  dynamicAnchors: {");
        foreach (var (anchorName, schemaHash) in anchorsToInclude)
        {
            var delegateExpr = requiresAnnotationTracking
                ? requiresRegistry
                    ? $"(data, scope, evaluatedState, location = \"\", registry = null) => validate_{schemaHash}(data, scope, evaluatedState, location, registry)"
                    : $"(data, scope, evaluatedState, location = \"\") => validate_{schemaHash}(data, scope, evaluatedState, location)"
                : requiresRegistry
                    ? $"(data, scope, location = \"\", registry = null) => validate_{schemaHash}(data, scope, location, registry)"
                    : $"(data, scope, location = \"\") => validate_{schemaHash}(data, scope, location)";
            sb.AppendLine($"    {JsLiteral.String(anchorName)}: {delegateExpr},");
        }
        sb.AppendLine("  }");
        sb.AppendLine("});");
        sb.AppendLine();
        return sb.ToString();
    }

    private static IReadOnlyList<FragmentValidatorInfo> CollectFragmentValidators(
        Dictionary<string, SubschemaInfo> uniqueSchemas,
        Uri? rootBaseUri,
        ISet<string> reachableHashes)
    {
        var result = new Dictionary<string, FragmentValidatorInfo>(StringComparer.Ordinal);
        foreach (var (hash, subschemaInfo) in uniqueSchemas)
        {
            if (!reachableHashes.Contains(hash) || string.IsNullOrEmpty(subschemaInfo.JsonPointerPath))
            {
                goto AnchorRegistration;
            }

            if (rootBaseUri != null)
            {
                var pointerUri = $"{rootBaseUri.AbsoluteUri}#{subschemaInfo.JsonPointerPath}";
                result.TryAdd(pointerUri, new FragmentValidatorInfo(hash, pointerUri));
            }

        AnchorRegistration:
            if (subschemaInfo.Schema.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var anchorBaseUri = subschemaInfo.EffectiveBaseUri;
            if (anchorBaseUri == null)
            {
                continue;
            }

            anchorBaseUri = new Uri(anchorBaseUri.GetLeftPart(UriPartial.Query));
            if (subschemaInfo.Schema.TryGetProperty("$anchor", out var anchorElement) &&
                anchorElement.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(anchorElement.GetString()))
            {
                var anchorUri = $"{anchorBaseUri.AbsoluteUri}#{anchorElement.GetString()}";
                result.TryAdd(anchorUri, new FragmentValidatorInfo(hash, anchorUri));
            }

            if (subschemaInfo.Schema.TryGetProperty("$dynamicAnchor", out var dynamicAnchorElement) &&
                dynamicAnchorElement.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(dynamicAnchorElement.GetString()))
            {
                var dynamicAnchorUri = $"{anchorBaseUri.AbsoluteUri}#{dynamicAnchorElement.GetString()}";
                result.TryAdd(dynamicAnchorUri, new FragmentValidatorInfo(hash, dynamicAnchorUri));
            }
        }

        return result.Values.ToList();
    }

    private static string? ExtractSchemaUri(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        if (schema.TryGetProperty("$id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
        {
            return idElement.GetString();
        }
        if (schema.TryGetProperty("id", out var legacyIdElement) && legacyIdElement.ValueKind == JsonValueKind.String)
        {
            return legacyIdElement.GetString();
        }
        return null;
    }

    private static string DeriveFileName(string? schemaUri, string? sourcePath)
    {
        // Only derive from the URI when it's absolute and hierarchical; relative
        // $ids like "person" or "schemas/person.json" are valid in JSON Schema but
        // cannot be parsed as absolute URIs. Fall back to sourcePath in that case.
        if (!string.IsNullOrEmpty(schemaUri) &&
            Uri.TryCreate(schemaUri, UriKind.Absolute, out var uri) &&
            uri.IsAbsoluteUri && !uri.IsFile)
        {
            var lastSegment = uri.Segments.LastOrDefault()?.TrimEnd('/');
            if (!string.IsNullOrEmpty(lastSegment))
            {
                return $"{SanitizeFileName(Path.GetFileNameWithoutExtension(lastSegment))}.js";
            }
        }
        if (!string.IsNullOrEmpty(sourcePath))
        {
            return $"{SanitizeFileName(Path.GetFileNameWithoutExtension(sourcePath))}.js";
        }
        return "validator.js";
    }

    private static string SanitizeFileName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
        }
        return sb.Length > 0 ? sb.ToString() : "validator";
    }

    private static bool ContainsExternalRef(JsonElement schema)
    {
        return schema.ValueKind == JsonValueKind.Object &&
               schema.TryGetProperty("$ref", out var refElem) &&
               refElem.ValueKind == JsonValueKind.String &&
               !string.IsNullOrEmpty(refElem.GetString()) &&
               !refElem.GetString()!.StartsWith('#');
    }
}
