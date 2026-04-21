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
    private readonly List<IJsKeywordCodeGenerator> _keywordGenerators;
    private readonly SubschemaExtractor _extractor = new();

    /// <summary>
    /// The default draft version to use when a schema doesn't have an explicit $schema keyword.
    /// If null, defaults to Draft 2020-12.
    /// </summary>
    public SchemaDraft? DefaultDraft { get; set; }

    /// <summary>
    /// The import specifier used for the shared JS runtime module.
    /// Defaults to a relative ESM import sibling to the emitted validator.
    /// </summary>
    public string RuntimeImportSpecifier { get; set; } = "./jsv-runtime.js";

    public JsSchemaCodeGenerator()
    {
        _keywordGenerators =
        [
            new JsRefCodeGenerator(),
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

            var methods = new StringBuilder();
            var runtimeImports = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                if (!reach.ReachableHashes.Contains(hash)) continue;
                methods.AppendLine(GenerateValidationFunction(subschemaInfo, uniqueSchemas, detectedDraft, runtimeImports));
                methods.AppendLine();
            }

            var module = GenerateModule(schemaUri, rootHash, methods.ToString(), runtimeImports);
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
        SortedSet<string> runtimeImports)
    {
        var context = CreateContext(subschemaInfo, allSchemas, detectedDraft);
        var sb = new StringBuilder();

        sb.AppendLine($"function validate_{subschemaInfo.Hash}(v) {{");

        if (subschemaInfo.Schema.ValueKind == JsonValueKind.True)
        {
            sb.AppendLine("  return true;");
            sb.AppendLine("}");
            return sb.ToString();
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
        SchemaDraft detectedDraft)
    {
        return new JsCodeGenerationContext
        {
            CurrentSchema = subschemaInfo.Schema,
            CurrentHash = subschemaInfo.Hash,
            GetSubschemaHash = element => SchemaHasher.ComputeHash(element),
            ResolveLocalRef = refValue => _extractor.ResolveLocalRef(refValue),
            ResolveLocalRefInResource = (refValue, resourceRoot) =>
                _extractor.ResolveLocalRefInResource(refValue, resourceRoot),
            ResourceRoot = subschemaInfo.ResourceRoot,
            GetSubschemaInfo = hash => allSchemas.TryGetValue(hash, out var info) ? info : null,
            DetectedDraft = detectedDraft
        };
    }

    private string GenerateModule(
        string? schemaUri,
        string rootHash,
        string methods,
        SortedSet<string> runtimeImports)
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
        sb.AppendLine($"export function validate(data) {{ return validate_{rootHash}(data); }}");
        sb.AppendLine();

        var schemaUriLiteral = string.IsNullOrEmpty(schemaUri)
            ? "null"
            : JsLiteral.String(schemaUri);
        sb.AppendLine($"export const schemaUri = {schemaUriLiteral};");
        sb.AppendLine();

        sb.AppendLine("export default { validate, schemaUri };");
        return sb.ToString();
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
}
