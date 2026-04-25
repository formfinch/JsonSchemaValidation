// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Keywords;

/// <summary>
/// Generates code for "$ref" references, both local and external.
/// Local references are resolved at compile time and inlined.
/// External references are resolved at initialization time via the registry.
/// </summary>
public sealed class RefCodeGenerator : ICSharpKeywordCodeGenerator
{
    public string Keyword => "$ref";
    public int Priority => 200; // Run very early - $ref replaces the entire schema in most drafts

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!schema.TryGetProperty("$ref", out var refElement))
        {
            return false;
        }

        if (refElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var refValue = refElement.GetString();
        return !string.IsNullOrEmpty(refValue);
    }

    public string GenerateCode(CSharpCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("$ref", out var refElement))
        {
            return string.Empty;
        }

        var refValue = refElement.GetString();
        if (string.IsNullOrEmpty(refValue))
        {
            return string.Empty;
        }

        // Local reference (starts with #)
        if (refValue.StartsWith('#'))
        {
            return GenerateLocalRefCode(context, refValue);
        }

        // Check if this is a self-reference (same base URI as the root schema's $id)
        // e.g., "urn:uuid:xxx#/$defs/bar" when root $id is "urn:uuid:xxx"
        // Use RootBaseUri for this check since local refs resolve against the root schema
        if (context.RootBaseUri != null && refValue.Contains('#'))
        {
            var hashIndex = refValue.IndexOf('#');
            var refBase = refValue[..hashIndex];
            var fragment = refValue[hashIndex..]; // includes the #

            // Try to parse the ref base as a URI and compare with RootBaseUri
            if (UriHelpers.TryCreateAbsoluteSchemaUri(refBase, out var refBaseUri))
            {
                // Compare URIs (ignoring fragment on both)
                var rootUriWithoutFragment = new Uri(context.RootBaseUri.GetLeftPart(UriPartial.Query));
                if (refBaseUri.Equals(rootUriWithoutFragment))
                {
                    // Same base URI as root - treat the fragment as a local reference
                    return GenerateLocalRefCode(context, fragment);
                }
            }
        }

        // External reference
        return GenerateExternalRefCode(context, refValue);
    }

    private static string GenerateLocalRefCode(CSharpCodeGenerationContext context, string refValue)
    {
        // Resolve the $ref within the current schema resource (not necessarily the root)
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
            // Cannot resolve - this shouldn't happen if SubschemaExtractor did its job
            return $"// WARNING: Could not resolve $ref: {refValue}";
        }

        // Get the hash of the target schema and generate a call to its validation method
        var targetHash = context.GetSubschemaHash(targetSchema.Value);

        return GenerateLocalRefCallWithScope(context, refValue, targetHash);
    }

    private static string GenerateExternalRefCode(CSharpCodeGenerationContext context, string refValue)
    {
        // Resolve the external URI
        Uri targetUri;
        if (UriHelpers.TryCreateAbsoluteSchemaUri(refValue, out var absoluteUri))
        {
            targetUri = absoluteUri;
        }
        else if (context.BaseUri != null && Uri.TryCreate(context.BaseUri, refValue, out var resolvedUri))
        {
            targetUri = resolvedUri;
        }
        else
        {
            // Cannot resolve - treat as absolute URI anyway
            try
            {
                targetUri = new Uri(refValue, UriKind.RelativeOrAbsolute);
            }
            catch
            {
                return $"// WARNING: Could not parse $ref URI: {refValue}";
            }
        }

        // Check if this resolves to an internal $id (a subschema within the same document)
        var targetUriWithoutFragment = new Uri(targetUri.GetLeftPart(UriPartial.Query));
        var internalSchema = context.ResolveInternalId(targetUriWithoutFragment.AbsoluteUri);
        if (internalSchema.HasValue)
        {
            JsonElement? targetSchema = internalSchema.Value;
            var fragment = targetUri.Fragment;
            if (!string.IsNullOrEmpty(fragment) && fragment != "#")
            {
                if (!fragment.StartsWith('#'))
                {
                    fragment = $"#{fragment}";
                }
                targetSchema = context.ResolveLocalRefInResource(fragment, internalSchema.Value);
            }

            if (!targetSchema.HasValue)
            {
                return $"// WARNING: Could not resolve $ref: {refValue}";
            }

            // This is an internal reference - generate a local method call
            var targetHash = context.GetSubschemaHash(targetSchema.Value);
            return GenerateLocalRefCallWithScope(context, refValue, targetHash, "internal $id");
        }

        // External refs with fragments - the fragment URI should be registered separately
        // in the registry (e.g., http://example.com/schema.json#/$defs/foo)
        //
        // Normalize the URI: if the fragment is just "#" (root reference), strip it
        // This ensures http://example.com/schema# matches http://example.com/schema
        if (targetUri.Fragment == "#")
        {
            targetUri = new Uri(targetUri.GetLeftPart(UriPartial.Query));
        }

        // Generate a unique field name based on the hash of the target URI
        var fieldName = $"_extRef_{GenerateFieldNameSuffix(targetUri.AbsoluteUri)}";

        // Register this external ref for field generation
        // Check if already registered (same target URI)
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
            // Reuse existing field name
            fieldName = existingRef.FieldName;
        }

        var e2 = context.ElementVariable;

        // Generate null check - if external ref wasn't initialized (no registry), validation fails
        // When scope tracking is enabled, try to use the scoped interface to pass the dynamic scope
        if (context.RequiresScopeTracking)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// External $ref: {refValue} (with scope propagation)");
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
                sb.AppendLine($"    if ({fieldName} is IEvaluatedStateAwareCompiledValidator {fieldName}_eval)");
                sb.AppendLine("    {");
                sb.AppendLine($"        {context.EvaluatedStateVariable}.MergeFromSnapshot({fieldName}_eval.GetEvaluatedStateSnapshot());");
                sb.AppendLine("    }");
            }

            return sb.ToString();
        }

        if (context.RequiresPropertyAnnotations || context.RequiresItemAnnotations)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// External $ref: {refValue}");
            sb.AppendLine($"if ({fieldName} == null || !{fieldName}.IsValid({e2})) return false;");
            sb.AppendLine($"if ({fieldName} is IEvaluatedStateAwareCompiledValidator {fieldName}_eval)");
            sb.AppendLine("{");
            sb.AppendLine($"    {context.EvaluatedStateVariable}.MergeFromSnapshot({fieldName}_eval.GetEvaluatedStateSnapshot());");
            sb.AppendLine("}");
            return sb.ToString();
        }

        return $"""
            // External $ref: {refValue}
            if ({fieldName} == null || !{fieldName}.IsValid({e2})) return false;
            """;
    }

    private static string GenerateLocalRefCallWithScope(CSharpCodeGenerationContext context, string refValue, string targetHash, string? suffix = null)
    {
        var commentSuffix = string.IsNullOrEmpty(suffix) ? string.Empty : $" ({suffix})";

        if (!context.RequiresScopeTracking)
        {
            return $"// $ref: {refValue}{commentSuffix}\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        var targetInfo = context.GetSubschemaInfo(targetHash);
        if (targetInfo == null)
        {
            return $"// $ref: {refValue}{commentSuffix}\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        // If we're staying within the same resource (or entering a resource root),
        // the target's own method will push anchors as needed.
        if (targetInfo.IsResourceRoot || targetInfo.ResourceRootHash == context.CurrentResourceRootHash)
        {
            return $"// $ref: {refValue}{commentSuffix}\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        var resourceRootInfo = context.GetSubschemaInfo(targetInfo.ResourceRootHash);
        if (resourceRootInfo == null || (resourceRootInfo.ResourceAnchors.Count == 0 && !resourceRootInfo.HasRecursiveAnchor))
        {
            return $"// $ref: {refValue}{commentSuffix}\nif (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        var scopeSuffix = targetHash[..8];
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"// $ref: {refValue}{commentSuffix} (push resource scope)");
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

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string GenerateFieldNameSuffix(string input)
    {
        // Generate a short hash suffix for field naming
        var hash = 0u;
        foreach (var c in input)
        {
            hash = (hash * 31) + c;
        }
        return hash.ToString("x8");
    }

    public IEnumerable<StaticFieldInfo> GetStaticFields(CSharpCodeGenerationContext context)
    {
        return [];
    }
}
