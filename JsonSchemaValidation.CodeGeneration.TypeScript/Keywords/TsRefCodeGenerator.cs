// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript.Keywords;

/// <summary>
/// Generates TypeScript code for "$ref" references. Local references are
/// resolved at generation time; external references are resolved from an
/// optional runtime registry passed to validate(data, registry).
/// </summary>
public sealed class TsRefCodeGenerator : ITsKeywordCodeGenerator
{
    public string Keyword => "$ref";
    public int Priority => 200;

    public bool CanGenerate(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object) return false;
        if (!schema.TryGetProperty("$ref", out var r)) return false;
        return r.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(r.GetString());
    }

    public string GenerateCode(TsCodeGenerationContext context)
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

    private static string GenerateLocalRefCode(TsCodeGenerationContext context, string refValue)
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
                "Could not resolve local $ref during TypeScript code generation.");
        }
        var hash = context.GetSubschemaHash(target.Value);
        return GenerateScopeAwareCall(context, refValue, hash);
    }

    private static string GenerateExternalRefCode(TsCodeGenerationContext context, string refValue)
    {
        if (!TryResolveUri(context, refValue, out var targetUri))
        {
            throw new InvalidOperationException("Could not resolve external $ref URI during TypeScript code generation.");
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
                throw new InvalidOperationException("Could not resolve internal $id $ref during TypeScript code generation.");
            }

            var targetHash = context.GetSubschemaHash(targetSchema.Value);
            return GenerateScopeAwareCall(context, refValue, targetHash);
        }

        if (targetUri.Fragment == "#")
        {
            targetUri = new Uri(targetUri.GetLeftPart(UriPartial.Query));
        }

        var uriLiteral = TsLiteral.String(targetUri.AbsoluteUri);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  const _refValidator = {context.RegistryExpr}?.tryGetValidator?.({uriLiteral}) ?? null;");
        sb.AppendLine("  if (_refValidator === null) return false;");
        EmitExternalDispatch(sb, context, "  ");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Emits a runtime dispatch over a resolved external _refValidator that
    /// picks the best stable method on the target validator based on the
    /// CALLER's feature needs. Method names on targets have fixed signatures:
    ///   validateWithScopeAndState(data, scope, evaluatedState, location [, registry])
    ///   validateWithState        (data, evaluatedState, location [, registry])
    ///   validateWithScope        (data, scope, location [, registry])
    ///   validate                 (data [, registry])
    /// The caller falls through to the simpler method when the target doesn't
    /// expose the richer form. Choices that drop scope or state leave the
    /// caller's _scope / _eval unchanged for that branch — which is
    /// spec-acceptable because the downgraded target can't produce the info
    /// we'd otherwise merge.
    /// </summary>
    internal static void EmitExternalDispatch(System.Text.StringBuilder sb, TsCodeGenerationContext context, string indent)
    {
        var v = context.ElementExpr;
        var scope = context.ScopeExpr;
        var state = context.EvaluatedStateExpr;
        var loc = context.LocationExpr;
        var reg = context.RegistryExpr;

        if (context.RequiresScopeTracking && context.RequiresAnnotationTracking)
        {
            sb.AppendLine($"{indent}const _refScopeAndState = typeof _refValidator === \"function\" ? null : _refValidator.validateWithScopeAndState;");
            sb.AppendLine($"{indent}const _refState = typeof _refValidator === \"function\" ? null : _refValidator.validateWithState;");
            sb.AppendLine($"{indent}const _refScope = typeof _refValidator === \"function\" ? null : _refValidator.validateWithScope;");
            sb.AppendLine($"{indent}if (typeof _refScopeAndState === \"function\") {{");
            sb.AppendLine($"{indent}  if (!_refScopeAndState({v}, {scope}, {state}, {loc}, {reg})) return false;");
            sb.AppendLine($"{indent}}} else if (typeof _refState === \"function\") {{");
            sb.AppendLine($"{indent}  if (!_refState({v}, {state}, {loc}, {reg})) return false;");
            sb.AppendLine($"{indent}}} else if (typeof _refScope === \"function\") {{");
            sb.AppendLine($"{indent}  if (!_refScope({v}, {scope}, {loc}, {reg})) return false;");
            sb.AppendLine($"{indent}}} else {{");
            sb.AppendLine($"{indent}  const _refValidate = typeof _refValidator === \"function\" ? _refValidator : _refValidator.validate;");
            sb.AppendLine($"{indent}  if (typeof _refValidate !== \"function\") return false;");
            sb.AppendLine($"{indent}  if (!_refValidate({v}, {reg})) return false;");
            sb.AppendLine($"{indent}}}");
        }
        else if (context.RequiresAnnotationTracking)
        {
            sb.AppendLine($"{indent}const _refState = typeof _refValidator === \"function\" ? null : _refValidator.validateWithState;");
            sb.AppendLine($"{indent}if (typeof _refState === \"function\") {{");
            sb.AppendLine($"{indent}  if (!_refState({v}, {state}, {loc}, {reg})) return false;");
            sb.AppendLine($"{indent}}} else {{");
            sb.AppendLine($"{indent}  const _refValidate = typeof _refValidator === \"function\" ? _refValidator : _refValidator.validate;");
            sb.AppendLine($"{indent}  if (typeof _refValidate !== \"function\") return false;");
            sb.AppendLine($"{indent}  if (!_refValidate({v}, {reg})) return false;");
            sb.AppendLine($"{indent}}}");
        }
        else if (context.RequiresScopeTracking)
        {
            sb.AppendLine($"{indent}const _refScope = typeof _refValidator === \"function\" ? null : _refValidator.validateWithScope;");
            sb.AppendLine($"{indent}if (typeof _refScope === \"function\") {{");
            sb.AppendLine($"{indent}  if (!_refScope({v}, {scope}, {loc}, {reg})) return false;");
            sb.AppendLine($"{indent}}} else {{");
            sb.AppendLine($"{indent}  const _refValidate = typeof _refValidator === \"function\" ? _refValidator : _refValidator.validate;");
            sb.AppendLine($"{indent}  if (typeof _refValidate !== \"function\") return false;");
            sb.AppendLine($"{indent}  if (!_refValidate({v}, {reg})) return false;");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            sb.AppendLine($"{indent}const _refValidate = typeof _refValidator === \"function\" ? _refValidator : _refValidator.validate;");
            sb.AppendLine($"{indent}if (typeof _refValidate !== \"function\") return false;");
            sb.AppendLine($"{indent}if (!_refValidate({v}, {reg})) return false;");
        }
    }

    private static bool TryResolveUri(TsCodeGenerationContext context, string refValue, out Uri targetUri)
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

    private static string GenerateScopeAwareCall(TsCodeGenerationContext context, string refValue, string targetHash)
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
            sb.AppendLine($"      {TsLiteral.String(anchorName)}: {delegateExpr},");
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

    public IEnumerable<string> GetRuntimeImports(TsCodeGenerationContext context) => [];
}
