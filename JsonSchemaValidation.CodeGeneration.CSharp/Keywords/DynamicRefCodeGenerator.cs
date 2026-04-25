// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for "$dynamicRef" references when they can be resolved statically.
/// When $dynamicRef points to a local anchor (#anchorName), it resolves statically
/// just like $ref. Dynamic resolution only occurs when there's an outer schema
/// with a matching $dynamicAnchor in the dynamic scope.
/// </summary>
public sealed class DynamicRefCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "$dynamicRef";
    public int Priority => 199; // Run right after $ref

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!schema.TryGetProperty("$dynamicRef", out var refElement))
        {
            return false;
        }

        if (refElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var refValue = refElement.GetString();
        if (string.IsNullOrEmpty(refValue))
        {
            return false;
        }

        // Allow both local and external references
        return true;
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
    {
        // $dynamicRef is Draft 2020-12 only
        if (context.DetectedDraft != SchemaDraft.Draft202012)
        {
            return string.Empty;
        }

        if (!context.CurrentSchema.TryGetProperty("$dynamicRef", out var refElement))
        {
            return string.Empty;
        }

        var refValue = refElement.GetString();
        if (string.IsNullOrEmpty(refValue))
        {
            return string.Empty;
        }

        // External or relative references (not starting with #)
        if (!refValue.StartsWith('#'))
        {
            return GenerateExternalDynamicRefCode(context, refValue);
        }

        // Handle JSON Pointer references (e.g., "#/$defs/foo")
        // These behave like $ref - resolve within current resource
        if (refValue.StartsWith("#/"))
        {
            return ResolveAsRef(context, refValue);
        }

        // Handle anchor references (e.g., "#anchorName")
        var anchorName = refValue[1..]; // Remove the leading #

        // Check if the current resource has a $dynamicAnchor with this name (bookend check)
        // This is required for dynamic resolution to be enabled
        JsonElement? localDynamicAnchor = null;
        if (context.ResourceRoot.HasValue)
        {
            localDynamicAnchor = FindDynamicAnchorInResource(anchorName, context.ResourceRoot.Value);
        }

        if (!localDynamicAnchor.HasValue)
        {
            // No bookend - behave like $ref, resolve locally (can use $anchor or $dynamicAnchor)
            return ResolveAsRef(context, refValue);
        }

        // Bookend exists - generate code that searches the dynamic scope at runtime.
        // Per JSON Schema 2020-12 section 8.2.3.2:
        // "the outermost schema resource in the dynamic scope that defines an identically named fragment"
        var localHash = context.GetSubschemaHash(localDynamicAnchor.Value);

        if (context.RequiresScopeTracking)
        {
            // Generate scope-aware resolution code
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// $dynamicRef: {refValue} (with dynamic scope resolution)");
            sb.AppendLine($"if ({context.ScopeVariable}.TryResolveDynamicAnchor(\"{anchorName}\", out var _dynValidator_{localHash[..8]}))");
            sb.AppendLine("{");
            sb.AppendLine($"    if (!_dynValidator_{localHash[..8]}!({context.ElementVariable}, {context.ScopeVariable}, {context.LocationVariable})) return false;");
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.AppendLine($"    if (!{context.GenerateValidateCall(localHash)}) return false;");
            sb.Append("}");
            return sb.ToString();
        }
        else
        {
            // Legacy path: no scope tracking, use static resolution or _dynamicScopeRoot fallback
            // Check for outer $dynamicAnchors at ROOT level (depth 0) only
            if (context.FindOuterDynamicAnchor != null && context.ResourceDepth > 0)
            {
                var outerAnchor = context.FindOuterDynamicAnchor(anchorName, 1);
                if (outerAnchor.HasValue)
                {
                    var targetHash = context.GetSubschemaHash(outerAnchor.Value);
                    return $"// $dynamicRef: {refValue} (resolved to outer $dynamicAnchor at root)\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
                }
            }

            // Fall back to _dynamicScopeRoot at runtime
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// $dynamicRef: {refValue} (with runtime scope check - legacy)");
            sb.AppendLine("if (_dynamicScopeRoot != null)");
            sb.AppendLine("{");
            sb.AppendLine("    if (!_dynamicScopeRoot.IsValid(e)) return false;");
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.AppendLine($"    if (!{context.GenerateValidateCall(localHash)}) return false;");
            sb.Append("}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Resolve the reference like a regular $ref (within current resource).
    /// </summary>
    private static string ResolveAsRef(CSharpCodeGenerationContext context, string refValue)
    {
        JsonElement? targetSchema;
        if (context.ResourceRoot.HasValue)
        {
            targetSchema = context.ResolveLocalRefInResource(refValue, context.ResourceRoot.Value);
        }
        else
        {
            targetSchema = context.ResolveLocalRef(refValue);
        }

        if (!targetSchema.HasValue)
        {
            return $"// WARNING: Could not resolve $dynamicRef: {refValue}";
        }

        var targetHash = context.GetSubschemaHash(targetSchema.Value);
        return GenerateLocalDynamicRefCallWithScope(context, refValue, targetHash, "resolved as $ref");
    }

    private static string GenerateExternalDynamicRefCode(CSharpCodeGenerationContext context, string refValue)
    {
        // Resolve the external URI
        if (!TryResolveUri(context, refValue, out var targetUri))
        {
            return $"// WARNING: Could not parse $dynamicRef URI: {refValue}";
        }

        var fragment = targetUri.Fragment;

        // If the fragment is a JSON pointer (or empty), treat as normal $ref
        if (string.IsNullOrEmpty(fragment) || fragment == "#" || fragment.StartsWith("#/", StringComparison.Ordinal))
        {
            return GenerateExternalRefLikeCode(context, refValue, targetUri);
        }

        // Anchor reference: "#anchorName"
        var anchorName = fragment[1..];

        // If this resolves to an internal $id (same document), treat as local and check bookend
        var targetUriWithoutFragment = new Uri(targetUri.GetLeftPart(UriPartial.Query));
        var internalResource = context.ResolveInternalId(targetUriWithoutFragment.AbsoluteUri);
        if (internalResource.HasValue)
        {
            var localTarget = context.ResolveLocalRefInResource(fragment, internalResource.Value);
            if (!localTarget.HasValue)
            {
                return $"// WARNING: Could not resolve $dynamicRef: {refValue}";
            }

            // Bookend check within the target resource
            var localDynamicAnchor = FindDynamicAnchorInResource(anchorName, internalResource.Value);
            if (!localDynamicAnchor.HasValue)
            {
                var targetHash = context.GetSubschemaHash(localTarget.Value);
                return GenerateLocalDynamicRefCallWithScope(context, refValue, targetHash, "resolved as $ref");
            }

            var localHash = context.GetSubschemaHash(localDynamicAnchor.Value);
            if (context.RequiresScopeTracking)
            {
                var fallbackCode = GenerateLocalDynamicRefCallWithScope(context, refValue, localHash, "resolved as $ref");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"// $dynamicRef: {refValue} (with dynamic scope resolution)");
                sb.AppendLine($"if ({context.ScopeVariable}.TryResolveDynamicAnchor(\"{anchorName}\", out var _dynValidator_{localHash[..8]}))");
                sb.AppendLine("{");
                sb.AppendLine($"    if (!_dynValidator_{localHash[..8]}!({context.ElementVariable}, {context.ScopeVariable}, {context.LocationVariable})) return false;");
                sb.AppendLine("}");
                sb.AppendLine("else");
                sb.AppendLine("{");
                foreach (var line in fallbackCode.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        sb.AppendLine($"    {line.TrimEnd()}");
                    }
                }
                sb.Append("}");
                return sb.ToString();
            }

            return GenerateLocalDynamicRefCallWithScope(context, refValue, localHash, "resolved as $ref");
        }

        // External reference - use registry-provided validator with runtime bookend check
        var fieldName = GetOrRegisterExternalRef(context, targetUri, refValue);
        var e2 = context.ElementVariable;

        if (context.RequiresScopeTracking)
        {
            var fieldSuffix = fieldName.Length > 8 ? fieldName[8..] : fieldName;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// External $dynamicRef: {refValue} (with dynamic scope resolution)");
            sb.AppendLine($"if ({fieldName} == null) return false;");
            if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
            {
                sb.AppendLine($"var _usedExternal_{fieldSuffix} = false;");
            }
            sb.AppendLine($"if ({fieldName} is IScopedCompiledValidator {fieldName}_scoped && {fieldName}_scoped.DynamicAnchors?.ContainsKey(\"{EscapeString(anchorName)}\") == true)");
            sb.AppendLine("{");
            sb.AppendLine($"    if ({context.ScopeVariable}.TryResolveDynamicAnchor(\"{EscapeString(anchorName)}\", out var _dynValidator_{fieldSuffix}))");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!_dynValidator_{fieldSuffix}!({e2}, {context.ScopeVariable}, {context.LocationVariable})) return false;");
            sb.AppendLine("    }");
            sb.AppendLine("    else");
            sb.AppendLine("    {");
            if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
            {
                sb.AppendLine($"        _usedExternal_{fieldSuffix} = true;");
            }
            sb.AppendLine($"        if (!{fieldName}_scoped.IsValid({e2}, {context.ScopeVariable})) return false;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.AppendLine($"    if ({fieldName} is IScopedCompiledValidator {fieldName}_fallbackScoped)");
            sb.AppendLine("    {");
            if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
            {
                sb.AppendLine($"        _usedExternal_{fieldSuffix} = true;");
            }
            sb.AppendLine($"        if (!{fieldName}_fallbackScoped.IsValid({e2}, {context.ScopeVariable})) return false;");
            sb.AppendLine("    }");
            sb.AppendLine("    else");
            sb.AppendLine("    {");
            if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
            {
                sb.AppendLine($"        _usedExternal_{fieldSuffix} = true;");
            }
            sb.AppendLine($"        if (!{fieldName}.IsValid({e2})) return false;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
            {
                sb.AppendLine($"if (_usedExternal_{fieldSuffix} && {fieldName} is IEvaluatedStateAwareCompiledValidator {fieldName}_eval)");
                sb.AppendLine("{");
                sb.AppendLine($"    {context.EvaluatedStateVariable}.MergeFromSnapshot({fieldName}_eval.GetEvaluatedStateSnapshot());");
                sb.AppendLine("}");
            }
            return sb.ToString();
        }

        if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// External $dynamicRef: {refValue}");
            sb.AppendLine($"if ({fieldName} == null || !{fieldName}.IsValid({e2})) return false;");
            sb.AppendLine($"if ({fieldName} is IEvaluatedStateAwareCompiledValidator {fieldName}_eval)");
            sb.AppendLine("{");
            sb.AppendLine($"    {context.EvaluatedStateVariable}.MergeFromSnapshot({fieldName}_eval.GetEvaluatedStateSnapshot());");
            sb.AppendLine("}");
            return sb.ToString();
        }

        return $"// External $dynamicRef: {refValue}\nif ({fieldName} == null || !{fieldName}.IsValid({e2})) return false;";
    }

    private static string GenerateExternalRefLikeCode(CSharpCodeGenerationContext context, string refValue, Uri targetUri)
    {
        var targetUriWithoutFragment = new Uri(targetUri.GetLeftPart(UriPartial.Query));
        var internalResource = context.ResolveInternalId(targetUriWithoutFragment.AbsoluteUri);
        if (internalResource.HasValue)
        {
            JsonElement? targetSchema = internalResource.Value;
            var fragment = targetUri.Fragment;
            if (!string.IsNullOrEmpty(fragment) && fragment != "#")
            {
                targetSchema = context.ResolveLocalRefInResource(fragment, internalResource.Value);
            }

            if (!targetSchema.HasValue)
            {
                return $"// WARNING: Could not resolve $dynamicRef: {refValue}";
            }

            var targetHash = context.GetSubschemaHash(targetSchema.Value);
            return GenerateLocalDynamicRefCallWithScope(context, refValue, targetHash, "resolved as $ref");
        }

        var fieldName = GetOrRegisterExternalRef(context, targetUri, refValue);
        var e2 = context.ElementVariable;

        if (context.RequiresScopeTracking)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// External $dynamicRef: {refValue}");
            sb.AppendLine($"if ({fieldName} == null) return false;");
            sb.AppendLine($"    if ({fieldName} is IScopedCompiledValidator {fieldName}_scoped)");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{fieldName}_scoped.IsValid({e2}, {context.ScopeVariable})) return false;");
            sb.AppendLine("    }");
            sb.AppendLine("    else");
            sb.AppendLine("    {");
            sb.AppendLine($"        if (!{fieldName}.IsValid({e2})) return false;");
            sb.AppendLine("    }");
            if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
            {
                sb.AppendLine($"if ({fieldName} is IEvaluatedStateAwareCompiledValidator {fieldName}_eval)");
                sb.AppendLine("{");
                sb.AppendLine($"    {context.EvaluatedStateVariable}.MergeFromSnapshot({fieldName}_eval.GetEvaluatedStateSnapshot());");
                sb.AppendLine("}");
            }
            return sb.ToString();
        }

        if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// External $dynamicRef: {refValue}");
            sb.AppendLine($"if ({fieldName} == null || !{fieldName}.IsValid({e2})) return false;");
            sb.AppendLine($"if ({fieldName} is IEvaluatedStateAwareCompiledValidator {fieldName}_eval)");
            sb.AppendLine("{");
            sb.AppendLine($"    {context.EvaluatedStateVariable}.MergeFromSnapshot({fieldName}_eval.GetEvaluatedStateSnapshot());");
            sb.AppendLine("}");
            return sb.ToString();
        }

        return $"// External $dynamicRef: {refValue}\nif ({fieldName} == null || !{fieldName}.IsValid({e2})) return false;";
    }

    private static string GenerateLocalDynamicRefCallWithScope(CSharpCodeGenerationContext context, string refValue, string targetHash, string suffix)
    {
        var commentSuffix = string.IsNullOrEmpty(suffix) ? string.Empty : $" ({suffix})";

        if (!context.RequiresScopeTracking)
        {
            return $"// $dynamicRef: {refValue}{commentSuffix}\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        var targetInfo = context.GetSubschemaInfo(targetHash);
        if (targetInfo == null)
        {
            return $"// $dynamicRef: {refValue}{commentSuffix}\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        if (targetInfo.IsResourceRoot || targetInfo.ResourceRootHash == context.CurrentResourceRootHash)
        {
            return $"// $dynamicRef: {refValue}{commentSuffix}\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        var resourceRootInfo = context.GetSubschemaInfo(targetInfo.ResourceRootHash);
        if (resourceRootInfo == null || (resourceRootInfo.ResourceAnchors.Count == 0 && !resourceRootInfo.HasRecursiveAnchor))
        {
            return $"// $dynamicRef: {refValue}{commentSuffix}\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        var scopeSuffix = targetHash[..8];
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"// $dynamicRef: {refValue}{commentSuffix} (push resource scope)");
        sb.AppendLine("{");
        sb.AppendLine($"var _scopeEntry_{scopeSuffix} = new CompiledScopeEntry");
        sb.AppendLine("{");
        if (resourceRootInfo.ResourceAnchors.Count > 0)
        {
            sb.AppendLine("    DynamicAnchors = new Dictionary<string, Func<JsonElement, ICompiledValidatorScope, string, bool>>(StringComparer.Ordinal)");
            sb.AppendLine("    {");
            foreach (var (anchorName, schemaHash) in resourceRootInfo.ResourceAnchors)
            {
                sb.AppendLine($"        [\"{EscapeString(anchorName)}\"] = {GetAnchorDelegateExpression(context, schemaHash)},");
            }
            sb.AppendLine("    },");
        }
        else
        {
            sb.AppendLine("    DynamicAnchors = null,");
        }
        sb.AppendLine($"    HasRecursiveAnchor = {(resourceRootInfo.HasRecursiveAnchor ? "true" : "false")},");
        if (resourceRootInfo.HasRecursiveAnchor)
        {
            sb.AppendLine($"    RootValidator = {GetAnchorDelegateExpression(context, resourceRootInfo.Hash)}");
        }
        else
        {
            sb.AppendLine("    RootValidator = null");
        }
        sb.AppendLine("};");
        sb.AppendLine($"var _scope_{scopeSuffix} = {context.ScopeVariable}.Push(_scopeEntry_{scopeSuffix});");
        sb.AppendLine($"if (!{GenerateValidateCallWithScope(context, targetHash, $"_scope_{scopeSuffix}")}) return false;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateValidateCallWithScope(CSharpCodeGenerationContext context, string hash, string scopeVariable)
    {
        // When scope tracking is enabled, always pass location for delegate signature compatibility
        var args = new List<string> { context.ElementVariable, scopeVariable };
        if (context.RequiresLocationTracking)
        {
            args.Add(context.LocationVariable);
        }
        else
        {
            args.Add("\"\""); // Empty location when not tracking but scope is enabled
        }
        return $"Validate_{hash}({string.Join(", ", args)})";
    }

    private static string GetAnchorDelegateExpression(CSharpCodeGenerationContext context, string hash)
    {
        // Delegate signature now includes location parameter: Func<JsonElement, ICompiledValidatorScope, string, bool>
        // The Validate_xxx method always matches this signature when scope tracking is enabled
        return $"Validate_{hash}";
    }

    private static bool TryResolveUri(CSharpCodeGenerationContext context, string refValue, out Uri targetUri)
    {
        if (UriHelpers.TryCreateAbsoluteSchemaUri(refValue, out var absoluteUri))
        {
            targetUri = absoluteUri;
            return true;
        }

        if (context.BaseUri != null && Uri.TryCreate(context.BaseUri, refValue, out var resolvedUri))
        {
            targetUri = resolvedUri;
            return true;
        }

        try
        {
            targetUri = new Uri(refValue, UriKind.RelativeOrAbsolute);
            return true;
        }
        catch
        {
            targetUri = null!;
            return false;
        }
    }

    private static string GetOrRegisterExternalRef(CSharpCodeGenerationContext context, Uri targetUri, string refValue)
    {
        var fieldName = $"_extRef_{GenerateFieldNameSuffix(targetUri.AbsoluteUri)}";

        var existingRef = context.ExternalRefs.Find(r => r.TargetUri.AbsoluteUri == targetUri.AbsoluteUri);
        if (existingRef == null)
        {
            context.ExternalRefs.Add(new ExternalRefInfo
            {
                FieldName = fieldName,
                TargetUri = targetUri,
                OriginalRef = refValue
            });
        }
        else
        {
            fieldName = existingRef.FieldName;
        }

        return fieldName;
    }

    private static string GenerateFieldNameSuffix(string input)
    {
        var hash = 0u;
        foreach (var c in input)
        {
            hash = (hash * 31) + c;
        }
        return hash.ToString("x8");
    }

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// Find a $dynamicAnchor with the given name within a schema resource.
    /// Only searches for $dynamicAnchor (not $anchor).
    /// </summary>
    private static JsonElement? FindDynamicAnchorInResource(string anchorName, JsonElement resourceRoot)
    {
        return FindDynamicAnchorInSchema(anchorName, resourceRoot);
    }

    /// <summary>
    /// Recursively search for a $dynamicAnchor within a schema.
    /// </summary>
    private static JsonElement? FindDynamicAnchorInSchema(string anchorName, JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Check if this schema has the $dynamicAnchor
        if (schema.TryGetProperty("$dynamicAnchor", out var dynamicAnchor) &&
            dynamicAnchor.ValueKind == JsonValueKind.String &&
            dynamicAnchor.GetString() == anchorName)
        {
            return schema;
        }

        // Search in subschema-containing keywords
        string[] objectKeywords = ["additionalProperties", "additionalItems", "items", "contains", "not", "if", "then", "else", "propertyNames", "unevaluatedProperties", "unevaluatedItems", "contentSchema"];
        string[] objectOfKeywords = ["properties", "patternProperties", "dependentSchemas", "$defs", "definitions"];
        string[] arrayKeywords = ["allOf", "anyOf", "oneOf", "prefixItems"];

        foreach (var keyword in objectKeywords)
        {
            if (schema.TryGetProperty(keyword, out var subschema))
            {
                // Skip if this subschema has its own $id (different resource)
                if (subschema.ValueKind == JsonValueKind.Object && subschema.TryGetProperty("$id", out _))
                {
                    continue;
                }

                var result = FindDynamicAnchorInSchema(anchorName, subschema);
                if (result.HasValue) return result;
            }
        }

        foreach (var keyword in objectOfKeywords)
        {
            if (schema.TryGetProperty(keyword, out var container) && container.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in container.EnumerateObject())
                {
                    // Skip if this subschema has its own $id (different resource)
                    if (prop.Value.ValueKind == JsonValueKind.Object && prop.Value.TryGetProperty("$id", out _))
                    {
                        continue;
                    }

                    var result = FindDynamicAnchorInSchema(anchorName, prop.Value);
                    if (result.HasValue) return result;
                }
            }
        }

        foreach (var keyword in arrayKeywords)
        {
            if (schema.TryGetProperty(keyword, out var array) && array.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in array.EnumerateArray())
                {
                    // Skip if this subschema has its own $id (different resource)
                    if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("$id", out _))
                    {
                        continue;
                    }

                    var result = FindDynamicAnchorInSchema(anchorName, item);
                    if (result.HasValue) return result;
                }
            }
        }

        return null;
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CSharpCodeGenerationContext context)
    {
        return [];
    }
}
