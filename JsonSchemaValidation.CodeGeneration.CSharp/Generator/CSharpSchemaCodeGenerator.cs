// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Linq;
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;
using FormFinch.JsonSchemaValidation.Common;
using static FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords.BooleanSchemaCodeGenerator;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;

/// <summary>
/// Main code generator that orchestrates the generation of compiled validators.
/// </summary>
public sealed class CSharpSchemaCodeGenerator
{
    private sealed record FragmentSubschemaInfo(
        string Hash,
        string FragmentUri,
        string ResourceRootHash,
        IReadOnlyList<(string AnchorName, string SchemaHash)> ResourceAnchors,
        bool HasRecursiveAnchor);

    private readonly List<ICSharpKeywordCodeGenerator> _keywordGenerators;
    private readonly SubschemaExtractor _extractor = new();

    /// <summary>
    /// Whether to use [GeneratedRegex] partial methods (true) or regular Regex fields (false).
    /// Set to true only for ahead-of-time compilation scenarios where source generators run.
    /// Defaults to false for compatibility with runtime compilation (Roslyn) and older TFMs.
    /// </summary>
    public bool UseGeneratedRegex { get; set; }

    /// <summary>
    /// Forces annotation tracking even when unevaluated* keywords are not present.
    /// Useful when compiled validators are used behind external $ref and their
    /// evaluated annotations must be merged by the caller.
    /// </summary>
    public bool ForceAnnotationTracking { get; set; }

    /// <summary>
    /// The default draft version to use when a schema doesn't have an explicit $schema keyword.
    /// If null, defaults to Draft 2020-12.
    /// Set this when compiling schemas from a known draft version (e.g., from test suites).
    /// </summary>
    public SchemaDraft? DefaultDraft { get; set; }

    public CSharpSchemaCodeGenerator()
    {
        // Register all keyword generators, ordered by priority (highest first)
        _keywordGenerators =
        [
            new BooleanSchemaCodeGenerator(),
            new RefCodeGenerator(),
            new DynamicRefCodeGenerator(),
            new RecursiveRefCodeGenerator(),
            new TypeCodeGenerator(),
            new RequiredCodeGenerator(),
            new EnumCodeGenerator(),
            new ConstCodeGenerator(),
            new StringConstraintsCodeGenerator(),
            new NumericConstraintsCodeGenerator(),
            new ArrayConstraintsCodeGenerator(),
            new ObjectConstraintsCodeGenerator(),
            new PrefixItemsCodeGenerator(),
            new ItemsCodeGenerator(),
            new AdditionalItemsCodeGenerator(),
            new ContainsCodeGenerator(),
            new PropertiesCodeGenerator(),
            new PatternPropertiesCodeGenerator(),
            new AdditionalPropertiesCodeGenerator(),
            new PropertyNamesCodeGenerator(),
            new DependenciesCodeGenerator(),
            new DependentRequiredCodeGenerator(),
            new DependentSchemasCodeGenerator(),
            new AllOfCodeGenerator(),
            new AnyOfCodeGenerator(),
            new OneOfCodeGenerator(),
            new NotCodeGenerator(),
            new ExtendsCodeGenerator(),    // Draft 3
            new DisallowCodeGenerator(),   // Draft 3
            new IfThenElseCodeGenerator(),
            new PatternCodeGenerator(),
            new FormatCodeGenerator(),
            new ContentCodeGenerator(),
            new UnevaluatedPropertiesCodeGenerator(),
            new UnevaluatedItemsCodeGenerator()
        ];

        _keywordGenerators.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// Generates a compiled validator from a schema file.
    /// </summary>
    public CSharpGenerationResult Generate(string schemaPath, string namespaceName, string? className = null)
    {
        try
        {
            var json = File.ReadAllText(schemaPath);
            using var doc = JsonDocument.Parse(json);
            return Generate(doc.RootElement, namespaceName, className, schemaPath);
        }
        catch (Exception ex)
        {
            return CSharpGenerationResult.Failed($"Failed to read schema: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a compiled validator from a schema element.
    /// </summary>
    public CSharpGenerationResult Generate(JsonElement schema, string namespaceName, string? className = null, string? sourcePath = null)
    {
        try
        {
            // Detect JSON Schema draft version - must be done first to validate schema is supported
            var draftResult = SchemaDraftDetector.DetectDraft(schema, DefaultDraft);
            if (!draftResult.Success)
            {
                return CSharpGenerationResult.Failed(draftResult.ErrorMessage!);
            }
            var detectedDraft = draftResult.Draft;

            // Extract schema URI for class naming and ICompiledValidator.SchemaUri
            var schemaUri = ExtractSchemaUri(schema);
            var resolvedClassName = className ?? DeriveClassName(schemaUri, sourcePath);

            // Determine base URI for resolving relative $refs
            Uri? baseUri = null;
            if (!string.IsNullOrEmpty(schemaUri))
            {
                Uri.TryCreate(schemaUri, UriKind.Absolute, out baseUri);
            }

            // Extract all unique subschemas (pass baseUri for $id resolution, defaultDraft for $ref/$id semantics)
            var uniqueSchemas = _extractor.ExtractUniqueSubschemas(schema, baseUri, DefaultDraft);
            var rootHash = SchemaHasher.ComputeHash(schema);

            // Check if annotation tracking is needed
            var requiresPropertyAnnotations = _extractor.HasUnevaluatedProperties || ForceAnnotationTracking;
            var requiresItemAnnotations = _extractor.HasUnevaluatedItems || ForceAnnotationTracking;

            // Collect all static fields and external refs
            var allStaticFields = new List<StaticFieldInfo>();
            var allExternalRefs = new List<ExternalRefInfo>();

            // Collect subschemas that should be registered by fragment URI
            // (e.g., #/$defs/foo should be registered as baseUri#/$defs/foo)
            var fragmentSubschemas = new List<FragmentSubschemaInfo>();
            if (baseUri != null)
            {
                foreach (var (hash, subschemaInfo) in uniqueSchemas)
                {
                    // Debug: Print all subschema paths (uncomment for debugging)
                    // Console.WriteLine($"Subschema {hash}: Path={subschemaInfo.JsonPointerPath ?? "(null)"}");

                    if (!string.IsNullOrEmpty(subschemaInfo.JsonPointerPath) && subschemaInfo.JsonPointerPath != "")
                    {
                        // Register subschemas under $defs (2019-09+) or definitions (Draft 4-7)
                        if (subschemaInfo.JsonPointerPath.StartsWith("/$defs/") ||
                            subschemaInfo.JsonPointerPath.StartsWith("/definitions/"))
                        {
                            var fragmentUri = $"{baseUri.AbsoluteUri}#{subschemaInfo.JsonPointerPath}";
                            if (!uniqueSchemas.TryGetValue(subschemaInfo.ResourceRootHash, out var resourceRootInfo))
                            {
                                fragmentSubschemas.Add(new FragmentSubschemaInfo(hash, fragmentUri, subschemaInfo.ResourceRootHash, [], false));
                            }
                            else
                            {
                                fragmentSubschemas.Add(new FragmentSubschemaInfo(
                                    hash,
                                    fragmentUri,
                                    subschemaInfo.ResourceRootHash,
                                    resourceRootInfo.ResourceAnchors,
                                    resourceRootInfo.HasRecursiveAnchor));
                            }
                        }
                    }
                }
            }

            var hasDynamicRef = detectedDraft == SchemaDraft.Draft202012 &&
                uniqueSchemas.Values.Any(s => s.Schema.ValueKind == JsonValueKind.Object &&
                                              s.Schema.TryGetProperty("$dynamicRef", out _));
            var hasRecursiveRef = detectedDraft == SchemaDraft.Draft201909 &&
                uniqueSchemas.Values.Any(s => s.Schema.ValueKind == JsonValueKind.Object &&
                                              s.Schema.TryGetProperty("$recursiveRef", out _));
            var hasDynamicAnchors = detectedDraft == SchemaDraft.Draft202012 &&
                uniqueSchemas.Values.Any(s => s.DynamicAnchors.Count > 0);
            var hasRecursiveAnchors = uniqueSchemas.Values.Any(s => s.HasRecursiveAnchor);

            // Enable scope tracking if any dynamic scope features are present.
            var requiresScopeTracking = hasDynamicRef || hasRecursiveRef || hasDynamicAnchors || hasRecursiveAnchors;

            // Pre-scan pass: detect external refs
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                var context = CreateContext(subschemaInfo, uniqueSchemas, baseUri, allExternalRefs, requiresPropertyAnnotations, requiresItemAnnotations, detectedDraft, requiresScopeTracking);

                // Draft 7 and earlier: $ref overrides all sibling keywords
                var refMasksSiblings = detectedDraft <= SchemaDraft.Draft7
                    && subschemaInfo.Schema.ValueKind == JsonValueKind.Object
                    && subschemaInfo.Schema.TryGetProperty("$ref", out _);

                foreach (var generator in _keywordGenerators)
                {
                    if (refMasksSiblings && generator.Keyword != "$ref")
                    {
                        continue;
                    }

                    if (generator.CanGenerate(subschemaInfo.Schema))
                    {
                        // This populates allExternalRefs via the context
                        _ = generator.GenerateCode(context);
                    }
                }
            }

            var hasExternalRefs = allExternalRefs.Count > 0;
            var hasFragmentSubschemas = fragmentSubschemas.Count > 0;
            var needsRegistryAware = hasExternalRefs || hasFragmentSubschemas;

            // Collect root resource anchors for IScopedCompiledValidator
            // This includes all $dynamicAnchors within the root schema resource (e.g., in $defs)
            var rootDynamicAnchors = requiresScopeTracking ? _extractor.GetRootResourceAnchors() : [];
            var hasRootRecursiveAnchor = requiresScopeTracking && _extractor.HasRootRecursiveAnchor();

            // Generate validation methods for each unique subschema
            var methods = new StringBuilder();
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                var methodCode = GenerateValidationMethod(subschemaInfo, uniqueSchemas, baseUri, allExternalRefs, needsRegistryAware, requiresPropertyAnnotations, requiresItemAnnotations, detectedDraft, requiresScopeTracking);
                methods.AppendLine(methodCode);
                methods.AppendLine();
            }

            // Collect static fields from all subschemas
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                var context = CreateContext(subschemaInfo, uniqueSchemas, baseUri, allExternalRefs, requiresPropertyAnnotations, requiresItemAnnotations, detectedDraft, requiresScopeTracking);

                // Draft 7 and earlier: $ref overrides all sibling keywords
                var refMasksSiblings = detectedDraft <= SchemaDraft.Draft7
                    && subschemaInfo.Schema.ValueKind == JsonValueKind.Object
                    && subschemaInfo.Schema.TryGetProperty("$ref", out _);

                foreach (var generator in _keywordGenerators)
                {
                    if (refMasksSiblings && generator.Keyword != "$ref")
                    {
                        continue;
                    }

                    if (generator.CanGenerate(subschemaInfo.Schema))
                    {
                        foreach (var field in generator.GetStaticFields(context))
                        {
                            // Avoid duplicate fields
                            if (!allStaticFields.Any(f => f.Name == field.Name))
                            {
                                allStaticFields.Add(field);
                            }
                        }
                    }
                }
            }

            // Generate the full class (registry-aware if needed for external refs, fragments, or dynamic scope)
            var code = GenerateClass(
                namespaceName,
                resolvedClassName,
                schemaUri,
                rootHash,
                allStaticFields,
                allExternalRefs,
                fragmentSubschemas,
                needsRegistryAware,
                methods.ToString(),
                requiresPropertyAnnotations,
                requiresItemAnnotations,
                UseGeneratedRegex,
                detectedDraft,
                requiresScopeTracking,
                rootDynamicAnchors,
                hasRootRecursiveAnchor,
                schema);

            var fileName = $"{resolvedClassName}.cs";
            return CSharpGenerationResult.Succeeded(code, fileName);
        }
        catch (Exception ex)
        {
            return CSharpGenerationResult.Failed($"Code generation failed: {ex.Message}");
        }
    }

    private string GenerateValidationMethod(SubschemaInfo subschemaInfo, Dictionary<string, SubschemaInfo> allSchemas, Uri? baseUri, List<ExternalRefInfo> externalRefs, bool needsRegistryAware, bool requiresPropertyAnnotations, bool requiresItemAnnotations, SchemaDraft detectedDraft, bool requiresScopeTracking)
    {
        var context = CreateContext(subschemaInfo, allSchemas, baseUri, externalRefs, requiresPropertyAnnotations, requiresItemAnnotations, detectedDraft, requiresScopeTracking);
        var sb = new StringBuilder();

        // Use instance methods if registry-aware or annotation tracking (need instance fields)
        var requiresInstance = needsRegistryAware || requiresPropertyAnnotations || requiresItemAnnotations;
        var hasAnnotationTracking = requiresPropertyAnnotations || requiresItemAnnotations;
        var staticModifier = requiresInstance ? "" : "static ";

        // Build parameter list
        // When scope tracking is enabled, always include the location parameter for delegate compatibility
        var paramList = new List<string> { "JsonElement e" };
        if (requiresScopeTracking)
        {
            paramList.Add("ICompiledValidatorScope _scope_");
            paramList.Add("string _loc_");
        }
        else if (hasAnnotationTracking)
        {
            paramList.Add("string _loc_");
        }
        sb.AppendLine($"    private {staticModifier}bool Validate_{subschemaInfo.Hash}({string.Join(", ", paramList)})");
        sb.AppendLine("    {");

        // Handle boolean schemas specially
        var booleanBody = BooleanSchemaCodeGenerator.GetBooleanSchemaBody(subschemaInfo.Schema);
        if (booleanBody != null)
        {
            sb.AppendLine($"        {booleanBody}");
            sb.AppendLine("    }");
            return sb.ToString();
        }

        // Push scope entry if this subschema has anchors (TASK-048 fix)
        // This ensures dynamic scope grows at resource boundaries with anchors
        // For resource roots: use ResourceAnchors (all anchors within the resource)
        // For non-resource roots: use DynamicAnchors (direct anchors only)
        if (requiresScopeTracking && subschemaInfo.ShouldPushScope)
        {
            sb.AppendLine("        // Push scope entry for this schema's anchors");
            sb.AppendLine("        var _scopeEntry_ = new CompiledScopeEntry");
            sb.AppendLine("        {");

            // Generate dynamic anchors dictionary
            // Resource roots include ALL anchors within the resource (not just direct ones)
            var anchorsToInclude = subschemaInfo.IsResourceRoot
                ? subschemaInfo.ResourceAnchors
                : subschemaInfo.DynamicAnchors.Select(name => (name, subschemaInfo.Hash)).ToList();

            if (anchorsToInclude.Count > 0)
            {
                sb.AppendLine("            DynamicAnchors = new Dictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>(StringComparer.Ordinal)");
                sb.AppendLine("            {");
                foreach (var (anchorName, schemaHash) in anchorsToInclude)
                {
                    // Map anchor name to the validation method of the schema containing that anchor
                    sb.AppendLine($"                [\"{EscapeString(anchorName)}\"] = {GetAnchorDelegateExpression(schemaHash, hasAnnotationTracking)},");
                }
                sb.AppendLine("            },");
            }
            else
            {
                sb.AppendLine("            DynamicAnchors = null,");
            }

            sb.AppendLine($"            HasRecursiveAnchor = {(subschemaInfo.HasRecursiveAnchor ? "true" : "false")},");

            // RootValidator points to this schema's validation method for $recursiveRef
            if (subschemaInfo.HasRecursiveAnchor)
            {
                sb.AppendLine($"            RootValidator = {GetAnchorDelegateExpression(subschemaInfo.Hash, hasAnnotationTracking)}");
            }
            else
            {
                sb.AppendLine("            RootValidator = null");
            }

            sb.AppendLine("        };");
            sb.AppendLine("        _scope_ = _scope_.Push(_scopeEntry_);");
            sb.AppendLine();
        }

        // Draft 7 and earlier: $ref overrides all sibling keywords
        var refMasksSiblings = detectedDraft <= SchemaDraft.Draft7
            && subschemaInfo.Schema.ValueKind == JsonValueKind.Object
            && subschemaInfo.Schema.TryGetProperty("$ref", out _);

        // Generate code for each applicable keyword
        foreach (var generator in _keywordGenerators)
        {
            if (refMasksSiblings && generator.Keyword != "$ref")
            {
                continue;
            }

            if (generator.CanGenerate(subschemaInfo.Schema))
            {
                var code = generator.GenerateCode(context);
                if (!string.IsNullOrWhiteSpace(code))
                {
                    // Indent the generated code
                    foreach (var line in code.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            sb.AppendLine($"        {line.TrimEnd()}");
                        }
                        else
                        {
                            sb.AppendLine();
                        }
                    }
                }
            }
        }

        sb.AppendLine("        return true;");
        sb.AppendLine("    }");

        return sb.ToString();
    }

    private CSharpCodeGenerationContext CreateContext(SubschemaInfo subschemaInfo, Dictionary<string, SubschemaInfo> allSchemas, Uri? baseUri, List<ExternalRefInfo> externalRefs, bool requiresPropertyAnnotations, bool requiresItemAnnotations, SchemaDraft detectedDraft, bool requiresScopeTracking)
    {
        // Use the subschema's effective base URI if available, otherwise fall back to root base URI
        var effectiveBaseUri = subschemaInfo.EffectiveBaseUri ?? baseUri;

        return new CSharpCodeGenerationContext
        {
            CurrentSchema = subschemaInfo.Schema,
            CurrentHash = subschemaInfo.Hash,
            GetSubschemaHash = element => SchemaHasher.ComputeHash(element),
            ResolveLocalRef = refValue => _extractor.ResolveLocalRef(refValue),
            ResolveInternalId = uri => _extractor.ResolveInternalId(uri),
            ResolveLocalRefInResource = (refValue, resourceRoot) => _extractor.ResolveLocalRefInResource(refValue, resourceRoot),
            GetSubschemaInfo = hash => allSchemas.TryGetValue(hash, out var info) ? info : null,
            ResourceRoot = subschemaInfo.ResourceRoot,
            CurrentResourceRootHash = subschemaInfo.ResourceRootHash,
            ResourceDepth = subschemaInfo.ResourceDepth,
            FindOutermostDynamicAnchor = anchorName => _extractor.FindOutermostDynamicAnchor(anchorName),
            FindOuterDynamicAnchor = (anchorName, depth) => _extractor.FindOuterDynamicAnchor(anchorName, depth),
            BaseUri = effectiveBaseUri,
            RootBaseUri = baseUri,
            ExternalRefs = externalRefs,
            RequiresPropertyAnnotations = requiresPropertyAnnotations,
            RequiresItemAnnotations = requiresItemAnnotations,
            UseGeneratedRegex = UseGeneratedRegex,
            DetectedDraft = detectedDraft,
            RequiresScopeTracking = requiresScopeTracking
        };
    }

    private string GenerateClass(
        string namespaceName,
        string className,
        string? schemaUri,
        string rootHash,
        List<StaticFieldInfo> staticFields,
        List<ExternalRefInfo> externalRefs,
        List<FragmentSubschemaInfo> fragmentSubschemas,
        bool needsRegistryAware,
        string methods,
        bool requiresPropertyAnnotations,
        bool requiresItemAnnotations,
        bool useGeneratedRegex,
        SchemaDraft detectedDraft,
        bool requiresScopeTracking,
        IReadOnlyList<(string Name, string Hash)> rootDynamicAnchors,
        bool hasRootRecursiveAnchor,
        JsonElement rootSchema)
    {
        var hasExternalRefs = externalRefs.Count > 0;
        var hasFragmentSubschemas = fragmentSubschemas.Count > 0;
        var hasAnnotationTracking = requiresPropertyAnnotations || requiresItemAnnotations;
        var sb = new StringBuilder();

        // File header
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by jsv-codegen.");
        sb.AppendLine("// Do not edit this file manually.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.RegularExpressions;");
        sb.AppendLine("using FormFinch.JsonSchemaValidation.Abstractions;");
        sb.AppendLine($"using FormFinch.JsonSchemaValidation.{SchemaDraftDetector.GetNamespace(detectedDraft)}.Keywords.Format;");
        if (needsRegistryAware || requiresScopeTracking)
        {
            sb.AppendLine("using FormFinch.JsonSchemaValidation.CompiledValidators;");
        }
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        // Class declaration - determine which interfaces to implement
        // Make class partial if we have GeneratedRegex fields and GeneratedRegex is enabled
        var hasGeneratedRegexFields = useGeneratedRegex && staticFields.Any(f => f.IsGeneratedRegex);
        var partialModifier = hasGeneratedRegexFields ? "partial " : "";
        var interfaces = new List<string>();
        if (requiresScopeTracking)
        {
            // IScopedCompiledValidator extends ICompiledValidator
            interfaces.Add("IScopedCompiledValidator");
        }
        if (needsRegistryAware)
        {
            // Need registry awareness for external refs or fragment subschemas
            interfaces.Add("IRegistryAwareCompiledValidator");
        }
        if (requiresPropertyAnnotations || requiresItemAnnotations)
        {
            interfaces.Add("IEvaluatedStateAwareCompiledValidator");
        }
        if (!interfaces.Contains("IScopedCompiledValidator") && !interfaces.Contains("ICompiledValidator"))
        {
            interfaces.Add("ICompiledValidator");
        }
        var interfaceName = string.Join(", ", interfaces);
        // Use internal for Generated namespace to avoid public API surface issues
        var accessModifier = namespaceName.Contains(".Generated") ? "internal" : "public";
        sb.AppendLine($"    {accessModifier} sealed {partialModifier}class {className} : {interfaceName}");
        sb.AppendLine("    {");

        // Static fields and GeneratedRegex methods
        if (staticFields.Count > 0)
        {
            foreach (var field in staticFields)
            {
                if (field.IsGeneratedRegex && useGeneratedRegex)
                {
                    // Generate [GeneratedRegex] attribute and partial method
                    var regexOptions = field.RegexOptions ?? "RegexOptions.None";
                    sb.AppendLine($"        [GeneratedRegex({field.Initializer}, {regexOptions}, matchTimeoutMilliseconds: {field.TimeoutMs})]");
                    sb.AppendLine($"        private static partial {field.Type} {field.Name}();");
                }
                else if (field.IsGeneratedRegex)
                {
                    // Generate regular Regex field for runtime compilation
                    // Add RegexOptions.Compiled for performance (GeneratedRegex already compiles at build time)
                    var baseOptions = field.RegexOptions ?? "RegexOptions.None";
                    var regexOptions = baseOptions == "RegexOptions.None"
                        ? "RegexOptions.Compiled"
                        : $"{baseOptions} | RegexOptions.Compiled";
                    sb.AppendLine($"        private static readonly {field.Type} {field.Name} = new {field.Type}({field.Initializer}, {regexOptions}, TimeSpan.FromMilliseconds({field.TimeoutMs}));");
                }
                else
                {
                    sb.AppendLine($"        private static readonly {field.Type} {field.Name} = {field.Initializer};");
                }
            }
            sb.AppendLine();
        }

        // Instance fields for external refs
        if (hasExternalRefs)
        {
            foreach (var extRef in externalRefs)
            {
                sb.AppendLine($"        private ICompiledValidator {extRef.FieldName} = null!;");
            }
            sb.AppendLine();
        }

        // Instance field for annotation tracking
        if (hasAnnotationTracking)
        {
            sb.AppendLine("        // Evaluation state for tracking annotations (unevaluatedProperties/unevaluatedItems)");
            sb.AppendLine("        private readonly EvaluatedState _eval_ = new();");
            sb.AppendLine();
        }

        // SchemaUri property
        if (!string.IsNullOrEmpty(schemaUri))
        {
            sb.AppendLine($"        public Uri SchemaUri => new Uri(\"{EscapeString(schemaUri)}\");");
        }
        else
        {
            sb.AppendLine("        public Uri SchemaUri => throw new NotSupportedException(\"Schema has no $id\");");
        }
        sb.AppendLine();

        // IScopedCompiledValidator properties
        if (requiresScopeTracking)
        {
            // DynamicAnchors property - dictionary of anchor names to validation functions
            if (rootDynamicAnchors.Count > 0)
            {
                sb.AppendLine("        private IReadOnlyDictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>? _dynamicAnchors;");
                sb.AppendLine();
                sb.AppendLine("        public IReadOnlyDictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>? DynamicAnchors");
                sb.AppendLine("        {");
                sb.AppendLine("            get");
                sb.AppendLine("            {");
                sb.AppendLine("                if (_dynamicAnchors == null)");
                sb.AppendLine("                {");
                sb.AppendLine("                    _dynamicAnchors = new Dictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>(StringComparer.Ordinal)");
                sb.AppendLine("                    {");
                foreach (var (name, hash) in rootDynamicAnchors)
                {
                    sb.AppendLine($"                        [\"{EscapeString(name)}\"] = {GetAnchorDelegateExpression(hash, hasAnnotationTracking)},");
                }
                sb.AppendLine("                    };");
                sb.AppendLine("                }");
                sb.AppendLine("                return _dynamicAnchors;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine("        public IReadOnlyDictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>? DynamicAnchors => null;");
            }
            sb.AppendLine();

            // HasRecursiveAnchor property
            sb.AppendLine($"        public bool HasRecursiveAnchor => {(hasRootRecursiveAnchor ? "true" : "false")};");
            sb.AppendLine();

            // RootValidator property - delegate to root validation method
            // Suppress heap allocation warning - this is an intentional delegate allocation on the public API boundary
            sb.AppendLine("#pragma warning disable HAA0603");
            sb.AppendLine($"        public Func<JsonElement, ICompiledValidatorScope, string, bool>? RootValidator => {GetAnchorDelegateExpression(rootHash, hasAnnotationTracking)};");
            sb.AppendLine("#pragma warning restore HAA0603");
            sb.AppendLine();
        }

        // RegisterSubschemas, Initialize, and SetDynamicScopeRoot methods for registry-aware validators
        if (needsRegistryAware)
        {
            // Field for dynamic scope root (for $dynamicRef resolution)
            sb.AppendLine("        private ICompiledValidator? _dynamicScopeRoot;");
            sb.AppendLine();

            // RegisterSubschemas - registers $defs subschemas for other validators to reference
            sb.AppendLine("        public void RegisterSubschemas(ICompiledValidatorRegistry registry)");
            sb.AppendLine("        {");
            if (hasFragmentSubschemas)
            {
                sb.AppendLine("            // Register subschemas by fragment URI so other validators can reference them");
                foreach (var fragment in fragmentSubschemas)
                {
                    sb.AppendLine($"            registry.RegisterForUri(new Uri(\"{EscapeString(fragment.FragmentUri)}\"), new SubschemaValidator_{fragment.Hash}(this));");
                }
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // Initialize - resolves external refs and sets up dynamic scope
            sb.AppendLine("        public void Initialize(ICompiledValidatorRegistry registry)");
            sb.AppendLine("        {");
            if (hasExternalRefs)
            {
                // Resolve external refs
                foreach (var extRef in externalRefs)
                {
                    sb.AppendLine($"            // External $ref: {extRef.OriginalRef}");
                    sb.AppendLine($"            if (!registry.TryGetValidator(new Uri(\"{EscapeString(extRef.TargetUri.AbsoluteUri)}\"), out var {extRef.FieldName}Resolved) || {extRef.FieldName}Resolved is null)");
                    sb.AppendLine($"                throw new InvalidOperationException(\"Failed to resolve $ref: {EscapeString(extRef.OriginalRef)}\");");
                    sb.AppendLine($"            {extRef.FieldName} = {extRef.FieldName}Resolved;");
                    // Set this validator as dynamic scope root for external refs (for $dynamicRef resolution)
                    sb.AppendLine($"            if ({extRef.FieldName} is IRegistryAwareCompiledValidator {extRef.FieldName}RegistryAware)");
                    sb.AppendLine($"                {extRef.FieldName}RegistryAware.SetDynamicScopeRoot(this);");
                }
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // SetDynamicScopeRoot - allows outer validators to set themselves as the dynamic scope root
            sb.AppendLine("        public void SetDynamicScopeRoot(ICompiledValidator? root)");
            sb.AppendLine("        {");
            sb.AppendLine("            _dynamicScopeRoot = root;");
            if (hasExternalRefs)
            {
                // Propagate to external refs
                foreach (var extRef in externalRefs)
                {
                    sb.AppendLine($"            if ({extRef.FieldName} is IRegistryAwareCompiledValidator {extRef.FieldName}RegistryAware)");
                    sb.AppendLine($"                {extRef.FieldName}RegistryAware.SetDynamicScopeRoot(root);");
                }
            }
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // IsValid entry points
        if (requiresScopeTracking)
        {
            // Scoped IsValid method (IScopedCompiledValidator)
            // When scope tracking is enabled, Validate methods always have location parameter
            if (hasAnnotationTracking)
            {
                sb.AppendLine("        public bool IsValid(JsonElement instance, ICompiledValidatorScope scope)");
                sb.AppendLine("        {");
                sb.AppendLine("            _eval_.Reset();");
                sb.AppendLine($"            return Validate_{rootHash}(instance, scope, \"\");");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine($"        public bool IsValid(JsonElement instance, ICompiledValidatorScope scope) => Validate_{rootHash}(instance, scope, \"\");");
            }
            sb.AppendLine();

            // Non-scoped IsValid creates initial scope with this validator's anchors
            sb.AppendLine("        public bool IsValid(JsonElement instance)");
            sb.AppendLine("        {");
            if (hasAnnotationTracking)
            {
                sb.AppendLine("            _eval_.Reset();");
            }
            sb.AppendLine("            var entry = new CompiledScopeEntry");
            sb.AppendLine("            {");
            sb.AppendLine("                DynamicAnchors = DynamicAnchors,");
            sb.AppendLine("                RootValidator = RootValidator,");
            sb.AppendLine("                HasRecursiveAnchor = HasRecursiveAnchor");
            sb.AppendLine("            };");
            sb.AppendLine("            var scope = CompiledValidatorScope.Empty.Push(entry);");
            // When scope tracking is enabled, Validate methods always have location parameter
            sb.AppendLine($"            return Validate_{rootHash}(instance, scope, \"\");");
            sb.AppendLine("        }");
        }
        else if (hasAnnotationTracking)
        {
            sb.AppendLine("        public bool IsValid(JsonElement instance)");
            sb.AppendLine("        {");
            sb.AppendLine("            _eval_.Reset();");
            sb.AppendLine($"            return Validate_{rootHash}(instance, \"\");");
            sb.AppendLine("        }");
        }
        else
        {
            sb.AppendLine($"        public bool IsValid(JsonElement instance) => Validate_{rootHash}(instance);");
        }
        sb.AppendLine();

        // Validation methods
        sb.Append(methods);

        // Generate DeepEquals helper if enum/const/uniqueItems are used in the schema
        if (methods.Contains("DeepEquals("))
        {
            sb.AppendLine();
            sb.AppendLine("        // Helper for deep equality comparison of JSON elements");
            sb.AppendLine("        private static bool DeepEquals(JsonElement left, JsonElement right)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (left.ValueKind != right.ValueKind) return false;");
            sb.AppendLine("            return left.ValueKind switch");
            sb.AppendLine("            {");
            sb.AppendLine("                JsonValueKind.Null => true,");
            sb.AppendLine("                JsonValueKind.True => true,");
            sb.AppendLine("                JsonValueKind.False => true,");
            sb.AppendLine("                JsonValueKind.String => string.Equals(left.GetString(), right.GetString(), StringComparison.Ordinal),");
            sb.AppendLine("                JsonValueKind.Number => CompareNumbers(left, right),");
            sb.AppendLine("                JsonValueKind.Array => CompareArrays(left, right),");
            sb.AppendLine("                JsonValueKind.Object => CompareObjects(left, right),");
            sb.AppendLine("                _ => false");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static bool CompareNumbers(JsonElement left, JsonElement right)");
            sb.AppendLine("        {");
            sb.AppendLine("            var leftRaw = left.GetRawText();");
            sb.AppendLine("            var rightRaw = right.GetRawText();");
            sb.AppendLine("            if (string.Equals(leftRaw, rightRaw, StringComparison.Ordinal)) return true;");
            sb.AppendLine("            if (left.TryGetDecimal(out var ld) && right.TryGetDecimal(out var rd)) return ld == rd;");
            sb.AppendLine("            if (left.TryGetDouble(out var ldd) && right.TryGetDouble(out var rdd)) return ldd.Equals(rdd);");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static bool CompareArrays(JsonElement left, JsonElement right)");
            sb.AppendLine("        {");
            sb.AppendLine("            var ll = left.GetArrayLength();");
            sb.AppendLine("            if (ll != right.GetArrayLength()) return false;");
            sb.AppendLine("            using var le = left.EnumerateArray();");
            sb.AppendLine("            using var re = right.EnumerateArray();");
            sb.AppendLine("            while (le.MoveNext() && re.MoveNext())");
            sb.AppendLine("            {");
            sb.AppendLine("                if (!DeepEquals(le.Current, re.Current)) return false;");
            sb.AppendLine("            }");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static bool CompareObjects(JsonElement left, JsonElement right)");
            sb.AppendLine("        {");
            sb.AppendLine("            var lc = 0; foreach (var _ in left.EnumerateObject()) lc++;");
            sb.AppendLine("            var rc = 0; foreach (var _ in right.EnumerateObject()) rc++;");
            sb.AppendLine("            if (lc != rc) return false;");
            sb.AppendLine("            foreach (var lp in left.EnumerateObject())");
            sb.AppendLine("            {");
            sb.AppendLine("                if (!right.TryGetProperty(lp.Name, out var rv)) return false;");
            sb.AppendLine("                if (!DeepEquals(lp.Value, rv)) return false;");
            sb.AppendLine("            }");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
        }

        // Generate JSON Pointer escape helper if annotation tracking is enabled
        if (hasAnnotationTracking)
        {
            sb.AppendLine();
            sb.AppendLine("        private static string EscapeJsonPointer(string segment)");
            sb.AppendLine("        {");
            sb.AppendLine("            return segment.Replace(\"~\", \"~0\").Replace(\"/\", \"~1\");");
            sb.AppendLine("        }");
        }

        // Expose evaluated annotations when annotation tracking is enabled
        if (hasAnnotationTracking)
        {
            sb.AppendLine();
            sb.AppendLine("        public EvaluatedStateSnapshot GetEvaluatedStateSnapshot()");
            sb.AppendLine("        {");
            sb.AppendLine("            return _eval_.ToSnapshot();");
            sb.AppendLine("        }");
        }

        // Generate EvaluatedState class if annotation tracking is enabled
        // Uses instance-location-aware tracking to properly handle nested objects/arrays
        if (hasAnnotationTracking)
        {
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Tracks which properties and items have been evaluated during validation.");
            sb.AppendLine("        /// Used by unevaluatedProperties and unevaluatedItems keywords.");
            sb.AppendLine("        /// Tracks by instance location (JSON Pointer) to properly handle nested structures.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        private sealed class EvaluatedState");
            sb.AppendLine("        {");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("            private readonly Dictionary<string, HashSet<string>> _evaluatedProperties = new(StringComparer.Ordinal);");
                sb.AppendLine();
                sb.AppendLine("            public HashSet<string> GetEvaluatedProperties(string loc)");
                sb.AppendLine("            {");
                sb.AppendLine("                if (!_evaluatedProperties.TryGetValue(loc, out var props))");
                sb.AppendLine("                {");
                sb.AppendLine("                    props = new HashSet<string>(StringComparer.Ordinal);");
                sb.AppendLine("                    _evaluatedProperties[loc] = props;");
                sb.AppendLine("                }");
                sb.AppendLine("                return props;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            public void MarkPropertyEvaluated(string loc, string propertyName)");
                sb.AppendLine("            {");
                sb.AppendLine("                GetEvaluatedProperties(loc).Add(propertyName);");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            public bool IsPropertyEvaluated(string loc, string propertyName)");
                sb.AppendLine("            {");
                sb.AppendLine("                return _evaluatedProperties.TryGetValue(loc, out var props) && props.Contains(propertyName);");
                sb.AppendLine("            }");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine();
                sb.AppendLine("            private readonly Dictionary<string, int> _evaluatedItemsUpTo = new(StringComparer.Ordinal);");
                sb.AppendLine("            private readonly Dictionary<string, HashSet<int>> _evaluatedItemIndices = new(StringComparer.Ordinal);");
                sb.AppendLine();
                sb.AppendLine("            public int GetEvaluatedItemsUpTo(string loc)");
                sb.AppendLine("            {");
                sb.AppendLine("                return _evaluatedItemsUpTo.TryGetValue(loc, out var upTo) ? upTo : 0;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            public void SetEvaluatedItemsUpTo(string loc, int upTo)");
                sb.AppendLine("            {");
                sb.AppendLine("                if (!_evaluatedItemsUpTo.TryGetValue(loc, out var current) || upTo > current)");
                sb.AppendLine("                    _evaluatedItemsUpTo[loc] = upTo;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            public HashSet<int> GetEvaluatedItemIndices(string loc)");
                sb.AppendLine("            {");
                sb.AppendLine("                if (!_evaluatedItemIndices.TryGetValue(loc, out var indices))");
                sb.AppendLine("                {");
                sb.AppendLine("                    indices = new HashSet<int>();");
                sb.AppendLine("                    _evaluatedItemIndices[loc] = indices;");
                sb.AppendLine("                }");
                sb.AppendLine("                return indices;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            public void MarkItemEvaluated(string loc, int index)");
                sb.AppendLine("            {");
                sb.AppendLine("                GetEvaluatedItemIndices(loc).Add(index);");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            public bool IsItemEvaluated(string loc, int index)");
                sb.AppendLine("            {");
                sb.AppendLine("                var upTo = GetEvaluatedItemsUpTo(loc);");
                sb.AppendLine("                if (index < upTo) return true;");
                sb.AppendLine("                return _evaluatedItemIndices.TryGetValue(loc, out var indices) && indices.Contains(index);");
                sb.AppendLine("            }");
            }
            sb.AppendLine();
            sb.AppendLine("            public void Reset()");
            sb.AppendLine("            {");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("                _evaluatedProperties.Clear();");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                _evaluatedItemsUpTo.Clear();");
                sb.AppendLine("                _evaluatedItemIndices.Clear();");
            }
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public EvaluatedState Clone()");
            sb.AppendLine("            {");
            sb.AppendLine("                var clone = new EvaluatedState();");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("                foreach (var kvp in _evaluatedProperties)");
                sb.AppendLine("                    clone._evaluatedProperties[kvp.Key] = new HashSet<string>(kvp.Value, StringComparer.Ordinal);");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                foreach (var kvp in _evaluatedItemsUpTo)");
                sb.AppendLine("                    clone._evaluatedItemsUpTo[kvp.Key] = kvp.Value;");
                sb.AppendLine("                foreach (var kvp in _evaluatedItemIndices)");
                sb.AppendLine("                    clone._evaluatedItemIndices[kvp.Key] = new HashSet<int>(kvp.Value);");
            }
            sb.AppendLine("                return clone;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void MergeFrom(EvaluatedState other)");
            sb.AppendLine("            {");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("                foreach (var kvp in other._evaluatedProperties)");
                sb.AppendLine("                {");
                sb.AppendLine("                    if (!_evaluatedProperties.TryGetValue(kvp.Key, out var props))");
                sb.AppendLine("                    {");
                sb.AppendLine("                        props = new HashSet<string>(StringComparer.Ordinal);");
                sb.AppendLine("                        _evaluatedProperties[kvp.Key] = props;");
                sb.AppendLine("                    }");
                sb.AppendLine("                    props.UnionWith(kvp.Value);");
                sb.AppendLine("                }");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                foreach (var kvp in other._evaluatedItemsUpTo)");
                sb.AppendLine("                {");
                sb.AppendLine("                    if (!_evaluatedItemsUpTo.TryGetValue(kvp.Key, out var current) || kvp.Value > current)");
                sb.AppendLine("                        _evaluatedItemsUpTo[kvp.Key] = kvp.Value;");
                sb.AppendLine("                }");
                sb.AppendLine("                foreach (var kvp in other._evaluatedItemIndices)");
                sb.AppendLine("                {");
                sb.AppendLine("                    if (!_evaluatedItemIndices.TryGetValue(kvp.Key, out var indices))");
                sb.AppendLine("                    {");
                sb.AppendLine("                        indices = new HashSet<int>();");
                sb.AppendLine("                        _evaluatedItemIndices[kvp.Key] = indices;");
                sb.AppendLine("                    }");
                sb.AppendLine("                    indices.UnionWith(kvp.Value);");
                sb.AppendLine("                }");
            }
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public EvaluatedStateSnapshot ToSnapshot()");
            sb.AppendLine("            {");
            sb.AppendLine("                var snapshot = new EvaluatedStateSnapshot();");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("                foreach (var kvp in _evaluatedProperties)");
                sb.AppendLine("                    snapshot.EvaluatedProperties[kvp.Key] = new HashSet<string>(kvp.Value, StringComparer.Ordinal);");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                foreach (var kvp in _evaluatedItemsUpTo)");
                sb.AppendLine("                    snapshot.EvaluatedItemsUpTo[kvp.Key] = kvp.Value;");
                sb.AppendLine("                foreach (var kvp in _evaluatedItemIndices)");
                sb.AppendLine("                    snapshot.EvaluatedItemIndices[kvp.Key] = new HashSet<int>(kvp.Value);");
            }
            sb.AppendLine("                return snapshot;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void MergeFromSnapshot(EvaluatedStateSnapshot snapshot)");
            sb.AppendLine("            {");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("                foreach (var kvp in snapshot.EvaluatedProperties)");
                sb.AppendLine("                {");
                sb.AppendLine("                    if (!_evaluatedProperties.TryGetValue(kvp.Key, out var props))");
                sb.AppendLine("                    {");
                sb.AppendLine("                        props = new HashSet<string>(StringComparer.Ordinal);");
                sb.AppendLine("                        _evaluatedProperties[kvp.Key] = props;");
                sb.AppendLine("                    }");
                sb.AppendLine("                    props.UnionWith(kvp.Value);");
                sb.AppendLine("                }");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                foreach (var kvp in snapshot.EvaluatedItemsUpTo)");
                sb.AppendLine("                {");
                sb.AppendLine("                    if (!_evaluatedItemsUpTo.TryGetValue(kvp.Key, out var current) || kvp.Value > current)");
                sb.AppendLine("                        _evaluatedItemsUpTo[kvp.Key] = kvp.Value;");
                sb.AppendLine("                }");
                sb.AppendLine("                foreach (var kvp in snapshot.EvaluatedItemIndices)");
                sb.AppendLine("                {");
                sb.AppendLine("                    if (!_evaluatedItemIndices.TryGetValue(kvp.Key, out var indices))");
                sb.AppendLine("                    {");
                sb.AppendLine("                        indices = new HashSet<int>();");
                sb.AppendLine("                        _evaluatedItemIndices[kvp.Key] = indices;");
                sb.AppendLine("                    }");
                sb.AppendLine("                    indices.UnionWith(kvp.Value);");
                sb.AppendLine("                }");
            }
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void SaveTo(out EvaluatedState snapshot)");
            sb.AppendLine("            {");
            sb.AppendLine("                snapshot = Clone();");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void RestoreFrom(EvaluatedState snapshot)");
            sb.AppendLine("            {");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("                _evaluatedProperties.Clear();");
                sb.AppendLine("                foreach (var kvp in snapshot._evaluatedProperties)");
                sb.AppendLine("                    _evaluatedProperties[kvp.Key] = new HashSet<string>(kvp.Value, StringComparer.Ordinal);");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                _evaluatedItemsUpTo.Clear();");
                sb.AppendLine("                foreach (var kvp in snapshot._evaluatedItemsUpTo)");
                sb.AppendLine("                    _evaluatedItemsUpTo[kvp.Key] = kvp.Value;");
                sb.AppendLine("                _evaluatedItemIndices.Clear();");
                sb.AppendLine("                foreach (var kvp in snapshot._evaluatedItemIndices)");
                sb.AppendLine("                    _evaluatedItemIndices[kvp.Key] = new HashSet<int>(kvp.Value);");
            }
            sb.AppendLine("            }");
            sb.AppendLine("        }");
        }

        // Generate inner wrapper classes for fragment subschemas
        if (hasFragmentSubschemas)
        {
            sb.AppendLine();
            foreach (var fragment in fragmentSubschemas)
            {
                var hash = fragment.Hash;
                var fragmentUri = fragment.FragmentUri;
                // Fragment validators implement IScopedCompiledValidator when scope tracking is needed
                // to properly propagate dynamic scope from callers
                if (requiresScopeTracking)
                {
                    sb.AppendLine($"        private sealed class SubschemaValidator_{hash} : IScopedCompiledValidator");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            private readonly {className} _parent;");
                    sb.AppendLine($"            public SubschemaValidator_{hash}({className} parent) => _parent = parent;");
                    sb.AppendLine($"            public Uri SchemaUri => new Uri(\"{EscapeString(fragmentUri)}\");");

                    // IScopedCompiledValidator properties - fragments don't declare their own anchors at the wrapper level
                    // (resource anchors are pushed when entering via this wrapper)
                    sb.AppendLine("            public IReadOnlyDictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>? DynamicAnchors => null;");
                    sb.AppendLine("            public bool HasRecursiveAnchor => false;");
                    sb.AppendLine("            public Func<JsonElement, ICompiledValidatorScope, string, bool>? RootValidator => null;");

                    // Scoped IsValid - propagates caller's scope
                    // When scope tracking is enabled, always pass location for delegate signature compatibility
                    var scopedCallArgs = new List<string> { "instance", "scope", "\"\"" };
                    sb.AppendLine("            public bool IsValid(JsonElement instance, ICompiledValidatorScope scope)");
                    sb.AppendLine("            {");
                    if (fragment.ResourceAnchors.Count > 0 || fragment.HasRecursiveAnchor)
                    {
                        var scopeSuffix = hash[..8];
                        sb.AppendLine($"                var _scopeEntry_{scopeSuffix} = new CompiledScopeEntry");
                        sb.AppendLine("                {");
                        if (fragment.ResourceAnchors.Count > 0)
                        {
                            sb.AppendLine("                    DynamicAnchors = new Dictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>(StringComparer.Ordinal)");
                            sb.AppendLine("                    {");
                            foreach (var (anchorName, schemaHash) in fragment.ResourceAnchors)
                            {
                                sb.AppendLine($"                        [\"{EscapeString(anchorName)}\"] = {GetAnchorDelegateExpression(schemaHash, hasAnnotationTracking, "_parent")},");
                            }
                            sb.AppendLine("                    },");
                        }
                        else
                        {
                            sb.AppendLine("                    DynamicAnchors = null,");
                        }
                        sb.AppendLine($"                    HasRecursiveAnchor = {(fragment.HasRecursiveAnchor ? "true" : "false")},");
                        if (fragment.HasRecursiveAnchor)
                        {
                            sb.AppendLine($"                    RootValidator = {GetAnchorDelegateExpression(fragment.ResourceRootHash, hasAnnotationTracking, "_parent")}");
                        }
                        else
                        {
                            sb.AppendLine("                    RootValidator = null");
                        }
                        sb.AppendLine("                };");
                        sb.AppendLine($"                var _scope_{scopeSuffix} = scope.Push(_scopeEntry_{scopeSuffix});");
                        scopedCallArgs[1] = $"_scope_{scopeSuffix}";
                    }
                    sb.AppendLine($"                return _parent.Validate_{hash}({string.Join(", ", scopedCallArgs)});");
                    sb.AppendLine("            }");

                    // Non-scoped IsValid - creates empty scope for standalone use
                    // When scope tracking is enabled, always pass location for delegate signature compatibility
                    var nonScopedCallArgs = new List<string> { "instance", "CompiledValidatorScope.Empty", "\"\"" };
                    sb.AppendLine("            public bool IsValid(JsonElement instance)");
                    sb.AppendLine("            {");
                    if (fragment.ResourceAnchors.Count > 0 || fragment.HasRecursiveAnchor)
                    {
                        var scopeSuffix = hash[..8];
                        sb.AppendLine($"                var _scopeEntry_{scopeSuffix} = new CompiledScopeEntry");
                        sb.AppendLine("                {");
                        if (fragment.ResourceAnchors.Count > 0)
                        {
                            sb.AppendLine("                    DynamicAnchors = new Dictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>(StringComparer.Ordinal)");
                            sb.AppendLine("                    {");
                            foreach (var (anchorName, schemaHash) in fragment.ResourceAnchors)
                            {
                                sb.AppendLine($"                        [\"{EscapeString(anchorName)}\"] = {GetAnchorDelegateExpression(schemaHash, hasAnnotationTracking, "_parent")},");
                            }
                            sb.AppendLine("                    },");
                        }
                        else
                        {
                            sb.AppendLine("                    DynamicAnchors = null,");
                        }
                        sb.AppendLine($"                    HasRecursiveAnchor = {(fragment.HasRecursiveAnchor ? "true" : "false")},");
                        if (fragment.HasRecursiveAnchor)
                        {
                            sb.AppendLine($"                    RootValidator = {GetAnchorDelegateExpression(fragment.ResourceRootHash, hasAnnotationTracking, "_parent")}");
                        }
                        else
                        {
                            sb.AppendLine("                    RootValidator = null");
                        }
                        sb.AppendLine("                };");
                        sb.AppendLine($"                var _scope_{scopeSuffix} = CompiledValidatorScope.Empty.Push(_scopeEntry_{scopeSuffix});");
                        nonScopedCallArgs[1] = $"_scope_{scopeSuffix}";
                    }
                    sb.AppendLine($"                return _parent.Validate_{hash}({string.Join(", ", nonScopedCallArgs)});");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                }
                else
                {
                    sb.AppendLine($"        private sealed class SubschemaValidator_{hash} : ICompiledValidator");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            private readonly {className} _parent;");
                    sb.AppendLine($"            public SubschemaValidator_{hash}({className} parent) => _parent = parent;");
                    sb.AppendLine($"            public Uri SchemaUri => new Uri(\"{EscapeString(fragmentUri)}\");");
                    var callArgs = new List<string> { "instance" };
                    if (hasAnnotationTracking)
                    {
                        callArgs.Add("\"\"");
                    }
                    sb.AppendLine($"            public bool IsValid(JsonElement instance) => _parent.Validate_{hash}({string.Join(", ", callArgs)});");
                    sb.AppendLine("        }");
                }
            }
        }

        // Close class and namespace
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string? ExtractSchemaUri(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Check for $id (Draft 2019-09 and later)
        if (schema.TryGetProperty("$id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
        {
            return idElement.GetString();
        }

        // Check for id (Draft 4 and earlier)
        if (schema.TryGetProperty("id", out var legacyIdElement) && legacyIdElement.ValueKind == JsonValueKind.String)
        {
            return legacyIdElement.GetString();
        }

        return null;
    }

    private static string DeriveClassName(string? schemaUri, string? sourcePath)
    {
        if (!string.IsNullOrEmpty(schemaUri))
        {
            // Extract last path segment from URI
            var uri = new Uri(schemaUri);
            var lastSegment = uri.Segments.LastOrDefault()?.TrimEnd('/');
            if (!string.IsNullOrEmpty(lastSegment))
            {
                return $"CompiledValidator_{SanitizeIdentifier(Path.GetFileNameWithoutExtension(lastSegment))}";
            }
        }

        if (!string.IsNullOrEmpty(sourcePath))
        {
            var fileName = Path.GetFileNameWithoutExtension(sourcePath);
            return $"CompiledValidator_{SanitizeIdentifier(fileName)}";
        }

        return "CompiledValidator";
    }

    private static string SanitizeIdentifier(string name)
    {
        var sb = new StringBuilder();
        var capitalizeNext = true;

        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        // Ensure it starts with a letter
        if (sb.Length > 0 && char.IsDigit(sb[0]))
        {
            sb.Insert(0, '_');
        }

        return sb.Length > 0 ? sb.ToString() : "Schema";
    }

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string GetAnchorDelegateExpression(string hash, bool hasAnnotationTracking, string? instancePrefix = null)
    {
        // The delegate signature now includes location: Func<JsonElement, ICompiledValidatorScope, string, bool>
        // The Validate_xxx method always matches this signature when scope tracking is enabled
        var prefix = string.IsNullOrEmpty(instancePrefix) ? string.Empty : $"{instancePrefix}.";
        return $"{prefix}Validate_{hash}";
    }
}

#pragma warning restore CS0618
