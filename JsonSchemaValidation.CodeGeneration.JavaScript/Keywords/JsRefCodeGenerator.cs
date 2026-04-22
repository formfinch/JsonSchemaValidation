// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Generates JavaScript code for "$ref" references. Local references are
/// resolved at generation time; external references are resolved from an
/// optional runtime registry passed to validate(data, registry).
/// </summary>
public sealed class JsRefCodeGenerator : IJsKeywordCodeGenerator
{
    public string Keyword => "$ref";
    public int Priority => 200;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return false;
        if (!schema.TryGetProperty("$ref", out var r)) return false;
        return r.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(r.GetString());
    }

    public string GenerateCode(JsCodeGenerationContext context)
    {
        if (!context.CurrentSchema.TryGetProperty("$ref", out var refElem)) return string.Empty;
        var refValue = refElem.GetString();
        if (string.IsNullOrEmpty(refValue)) return string.Empty;

        if (!refValue.StartsWith('#'))
        {
            return GenerateExternalRefCode(context, refValue);
        }

        return GenerateLocalRefCode(context, refValue);
    }

    private static string GenerateLocalRefCode(JsCodeGenerationContext context, string refValue)
    {
        var target = context.ResourceRoot.HasValue
            ? context.ResolveLocalRefInResource(refValue, context.ResourceRoot.Value)
            : context.ResolveLocalRef(refValue);
        if (!target.HasValue)
        {
            // An unresolved local ref at this point means the shared extractor
            // or the gate missed something — fail codegen loudly rather than
            // silently emitting "return false" that makes every instance fail.
            // The message deliberately omits refValue so a ref containing
            // newlines or other source-injection bait can't leak into output.
            throw new InvalidOperationException(
                "Could not resolve local $ref during JavaScript code generation.");
        }
        var hash = context.GetSubschemaHash(target.Value);
        return GenerateScopeAwareCall(context, refValue, hash);
    }

    private static string GenerateExternalRefCode(JsCodeGenerationContext context, string refValue)
    {
        if (!TryResolveUri(context, refValue, out var targetUri))
        {
            throw new InvalidOperationException("Could not resolve external $ref URI during JavaScript code generation.");
        }

        if (context.RootBaseUri != null && targetUri.Fragment.Length > 0)
        {
            var refBase = new Uri(targetUri.GetLeftPart(UriPartial.Query));
            var rootBase = new Uri(context.RootBaseUri.GetLeftPart(UriPartial.Query));
            if (refBase.Equals(rootBase))
            {
                return GenerateLocalRefCode(context, targetUri.Fragment);
            }
        }

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
                throw new InvalidOperationException("Could not resolve internal $id $ref during JavaScript code generation.");
            }

            var targetHash = context.GetSubschemaHash(targetSchema.Value);
            return GenerateScopeAwareCall(context, refValue, targetHash);
        }

        if (targetUri.Fragment == "#")
        {
            targetUri = new Uri(targetUri.GetLeftPart(UriPartial.Query));
        }

        var uriLiteral = JsLiteral.String(targetUri.AbsoluteUri);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  const _refValidator = {context.RegistryExpr}?.tryGetValidator?.({uriLiteral}) ?? null;");
        sb.AppendLine("  if (_refValidator === null) return false;");
        if (context.RequiresAnnotationTracking)
        {
            sb.AppendLine("  const _refValidateWithState = typeof _refValidator === \"function\" ? null : _refValidator.validateWithState;");
            sb.AppendLine("  const _refValidateWithScope = typeof _refValidator === \"function\" ? null : _refValidator.validateWithScope;");
            sb.AppendLine("  if (typeof _refValidateWithState === \"function\") {");
            if (context.RequiresScopeTracking)
            {
                sb.AppendLine($"    if (!_refValidateWithState({context.ElementExpr}, {context.ScopeExpr}, {context.EvaluatedStateExpr}, {context.LocationExpr}, {context.RegistryExpr})) return false;");
            }
            else
            {
                sb.AppendLine($"    if (!_refValidateWithState({context.ElementExpr}, {context.EvaluatedStateExpr}, {context.LocationExpr}, {context.RegistryExpr})) return false;");
            }
            sb.AppendLine("  } else if (typeof _refValidateWithScope === \"function\") {");
            if (context.RequiresScopeTracking)
            {
                sb.AppendLine($"    if (!_refValidateWithScope({context.ElementExpr}, {context.ScopeExpr}, {context.LocationExpr}, {context.RegistryExpr})) return false;");
            }
            else
            {
                sb.AppendLine($"    if (!_refValidateWithScope({context.ElementExpr}, {context.LocationExpr}, {context.RegistryExpr})) return false;");
            }
            sb.AppendLine("  } else {");
            sb.AppendLine("    const _refValidate = typeof _refValidator === \"function\" ? _refValidator : _refValidator.validate;");
            sb.AppendLine("    if (typeof _refValidate !== \"function\") return false;");
            sb.AppendLine($"    if (!_refValidate({context.ElementExpr}, {context.RegistryExpr})) return false;");
            sb.AppendLine("  }");
        }
        else if (context.RequiresScopeTracking)
        {
            sb.AppendLine("  const _refValidateWithScope = typeof _refValidator === \"function\" ? null : _refValidator.validateWithScope;");
            sb.AppendLine("  if (typeof _refValidateWithScope === \"function\") {");
            sb.AppendLine($"    if (!_refValidateWithScope({context.ElementExpr}, {context.ScopeExpr}, {context.LocationExpr}, {context.RegistryExpr})) return false;");
            sb.AppendLine("  } else {");
            sb.AppendLine("    const _refValidate = typeof _refValidator === \"function\" ? _refValidator : _refValidator.validate;");
            sb.AppendLine("    if (typeof _refValidate !== \"function\") return false;");
            sb.AppendLine($"    if (!_refValidate({context.ElementExpr}, {context.RegistryExpr})) return false;");
            sb.AppendLine("  }");
        }
        else
        {
            sb.AppendLine("  const _refValidate = typeof _refValidator === \"function\" ? _refValidator : _refValidator.validate;");
            sb.AppendLine("  if (typeof _refValidate !== \"function\") return false;");
            sb.AppendLine($"  if (!_refValidate({context.ElementExpr}, {context.RegistryExpr})) return false;");
        }
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

    private static string GenerateScopeAwareCall(JsCodeGenerationContext context, string refValue, string targetHash)
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

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  const _targetScope = _scope.push({");
        sb.AppendLine("    dynamicAnchors: {");
        foreach (var (anchorName, schemaHash) in resourceRootInfo.ResourceAnchors)
        {
            var delegateExpr = context.RequiresAnnotationTracking
                ? context.RequiresRegistry
                    ? $"(data, scope, evaluatedState, location = \"\", registry = null) => validate_{schemaHash}(data, scope, evaluatedState, location, registry)"
                    : $"(data, scope, evaluatedState, location = \"\") => validate_{schemaHash}(data, scope, evaluatedState, location)"
                : context.RequiresRegistry
                    ? $"(data, scope, location = \"\", registry = null) => validate_{schemaHash}(data, scope, location, registry)"
                    : $"(data, scope, location = \"\") => validate_{schemaHash}(data, scope, location)";
            sb.AppendLine($"      {JsLiteral.String(anchorName)}: {delegateExpr},");
        }
        sb.AppendLine("    }");
        sb.AppendLine("  });");
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
        sb.AppendLine($"  if (!validate_{targetHash}({string.Join(", ", args)})) return false;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public IEnumerable<string> GetRuntimeImports(JsCodeGenerationContext context) => [];
}
