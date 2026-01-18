using System.Text;
using System.Text.Json;
using JsonSchemaValidation.CodeGeneration.Keywords;
using JsonSchemaValidation.Common;
using static JsonSchemaValidation.CodeGeneration.Keywords.BooleanSchemaCodeGenerator;

namespace JsonSchemaValidation.CodeGeneration.Generator;

/// <summary>
/// Main code generator that orchestrates the generation of compiled validators.
/// </summary>
public sealed class SchemaCodeGenerator
{
    private readonly List<IKeywordCodeGenerator> _keywordGenerators;
    private readonly SubschemaExtractor _extractor = new();

    public SchemaCodeGenerator()
    {
        // Register all keyword generators, ordered by priority (highest first)
        _keywordGenerators =
        [
            new BooleanSchemaCodeGenerator(),
            new RefCodeGenerator(),
            new DynamicRefCodeGenerator(),
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
            new IfThenElseCodeGenerator(),
            new PatternCodeGenerator(),
            new FormatCodeGenerator(),
            new UnevaluatedPropertiesCodeGenerator(),
            new UnevaluatedItemsCodeGenerator()
        ];

        _keywordGenerators.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// Generates a compiled validator from a schema file.
    /// </summary>
    public GenerationResult Generate(string schemaPath, string namespaceName, string? className = null)
    {
        try
        {
            var json = File.ReadAllText(schemaPath);
            using var doc = JsonDocument.Parse(json);
            return Generate(doc.RootElement, namespaceName, className, schemaPath);
        }
        catch (Exception ex)
        {
            return GenerationResult.Failed($"Failed to read schema: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a compiled validator from a schema element.
    /// </summary>
    public GenerationResult Generate(JsonElement schema, string namespaceName, string? className = null, string? sourcePath = null)
    {
        try
        {
            // Extract schema URI for class naming and ICompiledValidator.SchemaUri
            var schemaUri = ExtractSchemaUri(schema);
            var resolvedClassName = className ?? DeriveClassName(schemaUri, sourcePath);

            // Determine base URI for resolving relative $refs
            Uri? baseUri = null;
            if (!string.IsNullOrEmpty(schemaUri))
            {
                Uri.TryCreate(schemaUri, UriKind.Absolute, out baseUri);
            }

            // Extract all unique subschemas (pass baseUri for $id resolution)
            var uniqueSchemas = _extractor.ExtractUniqueSubschemas(schema, baseUri);
            var rootHash = SchemaHasher.ComputeHash(schema);

            // Check if annotation tracking is needed
            var requiresPropertyAnnotations = _extractor.HasUnevaluatedProperties;
            var requiresItemAnnotations = _extractor.HasUnevaluatedItems;

            // Collect all static fields and external refs
            var allStaticFields = new List<StaticFieldInfo>();
            var allExternalRefs = new List<ExternalRefInfo>();

            // Pre-scan pass: detect external refs to determine if methods should be static
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                var context = CreateContext(subschemaInfo, uniqueSchemas, baseUri, allExternalRefs, requiresPropertyAnnotations, requiresItemAnnotations);
                foreach (var generator in _keywordGenerators)
                {
                    if (generator.CanGenerate(subschemaInfo.Schema))
                    {
                        // This populates allExternalRefs via the context
                        generator.GenerateCode(context);
                    }
                }
            }

            var hasExternalRefs = allExternalRefs.Count > 0;

            // Generate validation methods for each unique subschema
            var methods = new StringBuilder();
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                var methodCode = GenerateValidationMethod(subschemaInfo, uniqueSchemas, baseUri, allExternalRefs, hasExternalRefs, requiresPropertyAnnotations, requiresItemAnnotations);
                methods.AppendLine(methodCode);
                methods.AppendLine();
            }

            // Collect static fields from all subschemas
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                var context = CreateContext(subschemaInfo, uniqueSchemas, baseUri, allExternalRefs, requiresPropertyAnnotations, requiresItemAnnotations);
                foreach (var generator in _keywordGenerators)
                {
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

            // Generate the full class (registry-aware if there are external refs)
            var code = GenerateClass(
                namespaceName,
                resolvedClassName,
                schemaUri,
                rootHash,
                allStaticFields,
                allExternalRefs,
                methods.ToString(),
                requiresPropertyAnnotations,
                requiresItemAnnotations);

            var fileName = $"{resolvedClassName}.cs";
            return GenerationResult.Succeeded(code, fileName);
        }
        catch (Exception ex)
        {
            return GenerationResult.Failed($"Code generation failed: {ex.Message}");
        }
    }

    private string GenerateValidationMethod(SubschemaInfo subschemaInfo, Dictionary<string, SubschemaInfo> allSchemas, Uri? baseUri, List<ExternalRefInfo> externalRefs, bool hasExternalRefs, bool requiresPropertyAnnotations, bool requiresItemAnnotations)
    {
        var context = CreateContext(subschemaInfo, allSchemas, baseUri, externalRefs, requiresPropertyAnnotations, requiresItemAnnotations);
        var sb = new StringBuilder();

        // Use instance methods if there are external refs or annotation tracking (to access instance fields)
        var requiresInstance = hasExternalRefs || requiresPropertyAnnotations || requiresItemAnnotations;
        var staticModifier = requiresInstance ? "" : "static ";
        sb.AppendLine($"    private {staticModifier}bool Validate_{subschemaInfo.Hash}(JsonElement e)");
        sb.AppendLine("    {");

        // Handle boolean schemas specially
        var booleanBody = BooleanSchemaCodeGenerator.GetBooleanSchemaBody(subschemaInfo.Schema);
        if (booleanBody != null)
        {
            sb.AppendLine($"        {booleanBody}");
            sb.AppendLine("    }");
            return sb.ToString();
        }

        // Generate code for each applicable keyword
        foreach (var generator in _keywordGenerators)
        {
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

    private CodeGenerationContext CreateContext(SubschemaInfo subschemaInfo, Dictionary<string, SubschemaInfo> allSchemas, Uri? baseUri, List<ExternalRefInfo> externalRefs, bool requiresPropertyAnnotations, bool requiresItemAnnotations)
    {
        // Use the subschema's effective base URI if available, otherwise fall back to root base URI
        var effectiveBaseUri = subschemaInfo.EffectiveBaseUri ?? baseUri;

        return new CodeGenerationContext
        {
            CurrentSchema = subschemaInfo.Schema,
            CurrentHash = subschemaInfo.Hash,
            GetSubschemaHash = element => SchemaHasher.ComputeHash(element),
            ResolveLocalRef = refValue => _extractor.ResolveLocalRef(refValue),
            ResolveInternalId = uri => _extractor.ResolveInternalId(uri),
            ResolveLocalRefInResource = (refValue, resourceRoot) => _extractor.ResolveLocalRefInResource(refValue, resourceRoot),
            ResourceRoot = subschemaInfo.ResourceRoot,
            BaseUri = effectiveBaseUri,
            RootBaseUri = baseUri,
            ExternalRefs = externalRefs,
            RequiresPropertyAnnotations = requiresPropertyAnnotations,
            RequiresItemAnnotations = requiresItemAnnotations
        };
    }

    private string GenerateClass(
        string namespaceName,
        string className,
        string? schemaUri,
        string rootHash,
        List<StaticFieldInfo> staticFields,
        List<ExternalRefInfo> externalRefs,
        string methods,
        bool requiresPropertyAnnotations,
        bool requiresItemAnnotations)
    {
        var hasExternalRefs = externalRefs.Count > 0;
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
        sb.AppendLine("using JsonSchemaValidation.Abstractions;");
        sb.AppendLine("using JsonSchemaValidation.Draft202012.Keywords.Format;");
        if (hasExternalRefs)
        {
            sb.AppendLine("using JsonSchemaValidation.CompiledValidators;");
        }
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        // Class declaration - use IRegistryAwareCompiledValidator if there are external refs
        var interfaceName = hasExternalRefs ? "IRegistryAwareCompiledValidator" : "ICompiledValidator";
        sb.AppendLine($"    public sealed class {className} : {interfaceName}");
        sb.AppendLine("    {");

        // Static fields
        if (staticFields.Count > 0)
        {
            foreach (var field in staticFields)
            {
                sb.AppendLine($"        private static readonly {field.Type} {field.Name} = {field.Initializer};");
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

        // Initialize method for registry-aware validators
        if (hasExternalRefs)
        {
            sb.AppendLine("        public void Initialize(ICompiledValidatorRegistry registry)");
            sb.AppendLine("        {");
            foreach (var extRef in externalRefs)
            {
                sb.AppendLine($"            // External $ref: {extRef.OriginalRef}");
                sb.AppendLine($"            if (!registry.TryGetValidator(new Uri(\"{EscapeString(extRef.TargetUri.AbsoluteUri)}\"), out var {extRef.FieldName}Resolved) || {extRef.FieldName}Resolved is null)");
                sb.AppendLine($"                throw new InvalidOperationException(\"Failed to resolve $ref: {EscapeString(extRef.OriginalRef)}\");");
                sb.AppendLine($"            {extRef.FieldName} = {extRef.FieldName}Resolved;");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // IsValid entry point - reset evaluation state if annotation tracking is enabled
        if (hasAnnotationTracking)
        {
            sb.AppendLine("        public bool IsValid(JsonElement instance)");
            sb.AppendLine("        {");
            sb.AppendLine("            _eval_.Reset();");
            sb.AppendLine($"            return Validate_{rootHash}(instance);");
            sb.AppendLine("        }");
        }
        else
        {
            sb.AppendLine($"        public bool IsValid(JsonElement instance) => Validate_{rootHash}(instance);");
        }
        sb.AppendLine();

        // Validation methods
        sb.Append(methods);

        // Helper method for deep equality
        sb.AppendLine("        private static bool JsonElementDeepEquals(JsonElement a, JsonElement b)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (a.ValueKind != b.ValueKind) return false;");
        sb.AppendLine("            return a.ValueKind switch");
        sb.AppendLine("            {");
        sb.AppendLine("                JsonValueKind.Object => ObjectEquals(a, b),");
        sb.AppendLine("                JsonValueKind.Array => ArrayEquals(a, b),");
        sb.AppendLine("                JsonValueKind.String => a.GetString() == b.GetString(),");
        sb.AppendLine("                JsonValueKind.Number => a.GetDecimal() == b.GetDecimal(),");
        sb.AppendLine("                JsonValueKind.True => true,");
        sb.AppendLine("                JsonValueKind.False => true,");
        sb.AppendLine("                JsonValueKind.Null => true,");
        sb.AppendLine("                _ => false");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private static bool ObjectEquals(JsonElement a, JsonElement b)");
        sb.AppendLine("        {");
        sb.AppendLine("            var propsA = new Dictionary<string, JsonElement>();");
        sb.AppendLine("            foreach (var p in a.EnumerateObject()) propsA[p.Name] = p.Value;");
        sb.AppendLine("            var propsB = new Dictionary<string, JsonElement>();");
        sb.AppendLine("            foreach (var p in b.EnumerateObject()) propsB[p.Name] = p.Value;");
        sb.AppendLine("            if (propsA.Count != propsB.Count) return false;");
        sb.AppendLine("            foreach (var (key, val) in propsA)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (!propsB.TryGetValue(key, out var bVal)) return false;");
        sb.AppendLine("                if (!JsonElementDeepEquals(val, bVal)) return false;");
        sb.AppendLine("            }");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private static bool ArrayEquals(JsonElement a, JsonElement b)");
        sb.AppendLine("        {");
        sb.AppendLine("            var lenA = a.GetArrayLength();");
        sb.AppendLine("            var lenB = b.GetArrayLength();");
        sb.AppendLine("            if (lenA != lenB) return false;");
        sb.AppendLine("            using var enumA = a.EnumerateArray().GetEnumerator();");
        sb.AppendLine("            using var enumB = b.EnumerateArray().GetEnumerator();");
        sb.AppendLine("            while (enumA.MoveNext() && enumB.MoveNext())");
        sb.AppendLine("            {");
        sb.AppendLine("                if (!JsonElementDeepEquals(enumA.Current, enumB.Current)) return false;");
        sb.AppendLine("            }");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");

        // Generate EvaluatedState class if annotation tracking is enabled
        if (hasAnnotationTracking)
        {
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Tracks which properties and items have been evaluated during validation.");
            sb.AppendLine("        /// Used by unevaluatedProperties and unevaluatedItems keywords.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        private sealed class EvaluatedState");
            sb.AppendLine("        {");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("            public HashSet<string> EvaluatedProperties { get; } = new(StringComparer.Ordinal);");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("            public int EvaluatedItemsUpTo { get; set; }");
                sb.AppendLine("            public HashSet<int> EvaluatedItemIndices { get; } = new();");
            }
            sb.AppendLine();
            sb.AppendLine("            public void Reset()");
            sb.AppendLine("            {");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("                EvaluatedProperties.Clear();");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                EvaluatedItemsUpTo = 0;");
                sb.AppendLine("                EvaluatedItemIndices.Clear();");
            }
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public EvaluatedState Clone()");
            sb.AppendLine("            {");
            sb.AppendLine("                var clone = new EvaluatedState();");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("                foreach (var p in EvaluatedProperties) clone.EvaluatedProperties.Add(p);");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                clone.EvaluatedItemsUpTo = EvaluatedItemsUpTo;");
                sb.AppendLine("                foreach (var i in EvaluatedItemIndices) clone.EvaluatedItemIndices.Add(i);");
            }
            sb.AppendLine("                return clone;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void MergeFrom(EvaluatedState other)");
            sb.AppendLine("            {");
            if (requiresPropertyAnnotations)
            {
                sb.AppendLine("                foreach (var p in other.EvaluatedProperties) EvaluatedProperties.Add(p);");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                if (other.EvaluatedItemsUpTo > EvaluatedItemsUpTo) EvaluatedItemsUpTo = other.EvaluatedItemsUpTo;");
                sb.AppendLine("                foreach (var i in other.EvaluatedItemIndices) EvaluatedItemIndices.Add(i);");
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
                sb.AppendLine("                EvaluatedProperties.Clear();");
                sb.AppendLine("                foreach (var p in snapshot.EvaluatedProperties) EvaluatedProperties.Add(p);");
            }
            if (requiresItemAnnotations)
            {
                sb.AppendLine("                EvaluatedItemsUpTo = snapshot.EvaluatedItemsUpTo;");
                sb.AppendLine("                EvaluatedItemIndices.Clear();");
                sb.AppendLine("                foreach (var i in snapshot.EvaluatedItemIndices) EvaluatedItemIndices.Add(i);");
            }
            sb.AppendLine("            }");
            sb.AppendLine("        }");
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
}
