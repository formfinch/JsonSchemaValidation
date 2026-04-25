// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for "$dynamicRef" in Draft 2020-12.
/// </summary>
public sealed class JsDynamicRefCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "$dynamicRef";
    public int Priority => 199;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return false;
        if (!schema.TryGetProperty("$dynamicRef", out var r)) return false;
        return r.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(r.GetString());
    }

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (context.DetectedDraft != SchemaDraft.Draft202012 ||
            !context.CurrentSchema.TryGetProperty("$dynamicRef", out var refElem))
        {
            return string.Empty;
        }

        var refValue = refElem.GetString();
        if (string.IsNullOrEmpty(refValue))
        {
            return string.Empty;
        }

        if (refValue.StartsWith("#/"))
        {
            return GenerateLocalRefLikeCode(context, refValue);
        }

        if (refValue.StartsWith('#'))
        {
            return GenerateLocalDynamicRefCode(context, refValue[1..], refValue);
        }

        return GenerateExternalDynamicRefCode(context, refValue);
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];

    private static string GenerateLocalDynamicRefCode(JsCodeGenerationContext context, string anchorName, string refValue)
    {
        var localDynamicAnchor = context.ResourceRoot.HasValue
            ? FindDynamicAnchorInResource(anchorName, context.ResourceRoot.Value)
            : null;
        if (!localDynamicAnchor.HasValue)
        {
            return GenerateLocalRefLikeCode(context, refValue);
        }

        var localHash = context.GetSubschemaHash(localDynamicAnchor.Value);
        return GenerateDynamicLookupCode(context, anchorName, localHash);
    }

    private static string GenerateExternalDynamicRefCode(JsCodeGenerationContext context, string refValue)
    {
        if (!TryResolveUri(context, refValue, out var targetUri))
        {
            throw new InvalidOperationException("Could not resolve $dynamicRef URI during JavaScript code generation.");
        }

        var fragment = targetUri.Fragment;
        if (string.IsNullOrEmpty(fragment) || fragment == "#" || fragment.StartsWith("#/", StringComparison.Ordinal))
        {
            return GenerateExternalRefLikeCode(context, targetUri);
        }

        var anchorName = fragment[1..];
        var targetUriWithoutFragment = new Uri(targetUri.GetLeftPart(UriPartial.Query));
        var internalSchema = context.ResolveInternalId(targetUriWithoutFragment.AbsoluteUri);
        if (internalSchema.HasValue)
        {
            var localDynamicAnchor = FindDynamicAnchorInResource(anchorName, internalSchema.Value);
            if (!localDynamicAnchor.HasValue)
            {
                return GenerateExternalRefLikeCode(context, targetUri);
            }

            var localHash = context.GetSubschemaHash(localDynamicAnchor.Value);
            return GenerateDynamicLookupCode(context, anchorName, localHash);
        }

        var uriLiteral = JsLiteral.String(targetUri.AbsoluteUri);
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  const _dynValidator = {context.ScopeExpr}.tryResolveDynamicAnchor({JsLiteral.String(anchorName)});");
        sb.AppendLine("  if (_dynValidator !== null) {");
        sb.AppendLine($"    if (!{BuildDynamicValidatorCall(context, "_dynValidator")}) return false;");
        sb.AppendLine("  } else {");
        sb.AppendLine($"    const _refValidator = {context.RegistryExpr}?.tryGetValidator?.({uriLiteral}) ?? null;");
        sb.AppendLine("    if (_refValidator === null) return false;");
        JsRefCodeGenerator.EmitExternalDispatch(sb, context, "    ");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateLocalRefLikeCode(JsCodeGenerationContext context, string refValue)
    {
        JsonElement? target = context.ResourceRoot.HasValue
            ? context.ResolveLocalRefInResource(refValue, context.ResourceRoot.Value)
            : context.ResolveLocalRef(refValue);
        if (!target.HasValue)
        {
            throw new InvalidOperationException("Could not resolve $dynamicRef during JavaScript code generation.");
        }

        return GenerateScopeAwareCall(context, context.GetSubschemaHash(target.Value));
    }

    private static string GenerateExternalRefLikeCode(JsCodeGenerationContext context, Uri targetUri)
    {
        var targetUriWithoutFragment = new Uri(targetUri.GetLeftPart(UriPartial.Query));
        var internalSchema = context.ResolveInternalId(targetUriWithoutFragment.AbsoluteUri);
        if (internalSchema.HasValue)
        {
            JsonElement? targetSchema = internalSchema.Value;
            var fragment = targetUri.Fragment;
            if (!string.IsNullOrEmpty(fragment) && fragment != "#")
            {
                targetSchema = context.ResolveLocalRefInResource(fragment, internalSchema.Value);
            }

            if (!targetSchema.HasValue)
            {
                throw new InvalidOperationException("Could not resolve internal $dynamicRef during JavaScript code generation.");
            }

            return GenerateScopeAwareCall(context, context.GetSubschemaHash(targetSchema.Value));
        }

        if (targetUri.Fragment == "#")
        {
            targetUri = new Uri(targetUri.GetLeftPart(UriPartial.Query));
        }

        var uriLiteral = JsLiteral.String(targetUri.AbsoluteUri);
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  const _refValidator = {context.RegistryExpr}?.tryGetValidator?.({uriLiteral}) ?? null;");
        sb.AppendLine("  if (_refValidator === null) return false;");
        JsRefCodeGenerator.EmitExternalDispatch(sb, context, "  ");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateDynamicLookupCode(JsCodeGenerationContext context, string anchorName, string localHash)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  const _dynValidator = {context.ScopeExpr}.tryResolveDynamicAnchor({JsLiteral.String(anchorName)});");
        sb.AppendLine("  if (_dynValidator !== null) {");
        sb.AppendLine($"    if (!{BuildDynamicValidatorCall(context, "_dynValidator")}) return false;");
        sb.AppendLine("  } else {");
        foreach (var line in GenerateScopeAwareCall(context, localHash).Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine($"    {line.TrimEnd()}");
            }
        }
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildDynamicValidatorCall(JsCodeGenerationContext context, string validatorExpr)
    {
        var args = new List<string> { context.ElementExpr, context.ScopeExpr };
        if (context.RequiresAnnotationTracking)
        {
            args.Add(context.EvaluatedStateExpr);
        }
        args.Add(context.LocationExpr);
        if (context.RequiresRegistry)
        {
            args.Add(context.RegistryExpr);
        }
        return $"{validatorExpr}({string.Join(", ", args)})";
    }

    private static string GenerateScopeAwareCall(JsCodeGenerationContext context, string targetHash)
    {
        if (!context.RequiresScopeTracking)
        {
            return $"if (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        var targetInfo = context.GetSubschemaInfo(targetHash);
        if (targetInfo == null ||
            targetInfo.IsResourceRoot ||
            targetInfo.ResourceRootHash == context.CurrentResourceRootHash)
        {
            return $"if (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        var resourceRootInfo = context.GetSubschemaInfo(targetInfo.ResourceRootHash);
        if (resourceRootInfo == null || resourceRootInfo.ResourceAnchors.Count == 0)
        {
            return $"if (!{context.GenerateValidateCall(targetHash)}) return false;";
        }

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("const _targetScope = _scope.push({");
        sb.AppendLine("  dynamicAnchors: {");
        foreach (var (anchorName, schemaHash) in resourceRootInfo.ResourceAnchors)
        {
            var delegateExpr = context.RequiresAnnotationTracking
                ? context.RequiresRegistry
                    ? $"(data, scope, evaluatedState, location = \"\", registry = null) => validate_{schemaHash}(data, scope, evaluatedState, location, registry)"
                    : $"(data, scope, evaluatedState, location = \"\") => validate_{schemaHash}(data, scope, evaluatedState, location)"
                : context.RequiresRegistry
                    ? $"(data, scope, location = \"\", registry = null) => validate_{schemaHash}(data, scope, location, registry)"
                    : $"(data, scope, location = \"\") => validate_{schemaHash}(data, scope, location)";
            sb.AppendLine($"    {JsLiteral.String(anchorName)}: {delegateExpr},");
        }
        sb.AppendLine("  }");
        sb.AppendLine("});");
        var args = new List<string> { context.ElementExpr, "_targetScope" };
        if (context.RequiresAnnotationTracking)
        {
            args.Add(context.EvaluatedStateExpr);
        }
        args.Add(context.LocationExpr);
        if (context.RequiresRegistry)
        {
            args.Add(context.RegistryExpr);
        }
        sb.AppendLine($"if (!validate_{targetHash}({string.Join(", ", args)})) return false;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static bool TryResolveUri(JsCodeGenerationContext context, string refValue, out Uri targetUri)
    {
        if (Uri.TryCreate(refValue, UriKind.Absolute, out var absoluteUri))
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
            return targetUri.IsAbsoluteUri;
        }
        catch
        {
            targetUri = null!;
            return false;
        }
    }

    private static JsonElement? FindDynamicAnchorInResource(string anchorName, JsonElement resourceRoot)
    {
        return FindDynamicAnchorInSchema(anchorName, resourceRoot);
    }

    private static JsonElement? FindDynamicAnchorInSchema(string anchorName, JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (schema.TryGetProperty("$dynamicAnchor", out var dynamicAnchor) &&
            dynamicAnchor.ValueKind == JsonValueKind.String &&
            dynamicAnchor.GetString() == anchorName)
        {
            return schema;
        }

        foreach (var property in schema.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                if (property.Value.TryGetProperty("$id", out var idElement) &&
                    idElement.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrEmpty(idElement.GetString()) &&
                    !idElement.GetString()!.StartsWith('#'))
                {
                    continue;
                }

                var result = FindDynamicAnchorInSchema(anchorName, property.Value);
                if (result.HasValue) return result;
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (item.TryGetProperty("$id", out var idElement) &&
                        idElement.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrEmpty(idElement.GetString()) &&
                        !idElement.GetString()!.StartsWith('#'))
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
}
