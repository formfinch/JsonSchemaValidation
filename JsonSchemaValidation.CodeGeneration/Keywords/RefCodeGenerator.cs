using System.Text.Json;

namespace JsonSchemaValidation.CodeGeneration.Keywords;

/// <summary>
/// Generates code for "$ref" references, both local and external.
/// Local references are resolved at compile time and inlined.
/// External references are resolved at initialization time via the registry.
/// </summary>
public sealed class RefCodeGenerator : IKeywordCodeGenerator
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

    public string GenerateCode(CodeGenerationContext context)
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

        // Check if this is a self-reference (same base URI as the schema's $id)
        // e.g., "urn:uuid:xxx#/$defs/bar" when $id is "urn:uuid:xxx"
        if (context.BaseUri != null && refValue.Contains('#'))
        {
            var hashIndex = refValue.IndexOf('#');
            var refBase = refValue[..hashIndex];
            var fragment = refValue[hashIndex..]; // includes the #

            // Try to parse the ref base as a URI and compare with BaseUri
            if (Uri.TryCreate(refBase, UriKind.Absolute, out var refBaseUri))
            {
                // Compare URIs (ignoring fragment on both)
                var baseUriWithoutFragment = new Uri(context.BaseUri.GetLeftPart(UriPartial.Query));
                if (refBaseUri.Equals(baseUriWithoutFragment))
                {
                    // Same base URI - treat the fragment as a local reference
                    return GenerateLocalRefCode(context, fragment);
                }
            }
        }

        // External reference
        return GenerateExternalRefCode(context, refValue);
    }

    private static string GenerateLocalRefCode(CodeGenerationContext context, string refValue)
    {
        // Resolve the $ref to get the target schema
        var targetSchema = context.ResolveLocalRef(refValue);
        if (!targetSchema.HasValue)
        {
            // Cannot resolve - this shouldn't happen if SubschemaExtractor did its job
            return $"// WARNING: Could not resolve $ref: {refValue}";
        }

        // Get the hash of the target schema and generate a call to its validation method
        var targetHash = context.GetSubschemaHash(targetSchema.Value);
        var e = context.ElementVariable;

        return $"// $ref: {refValue}\nif (!Validate_{targetHash}({e})) return false;";
    }

    private static string GenerateExternalRefCode(CodeGenerationContext context, string refValue)
    {
        // Resolve the external URI
        Uri targetUri;
        if (Uri.TryCreate(refValue, UriKind.Absolute, out var absoluteUri))
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

        // External refs with fragments are not yet supported (would require subschema registration)
        // Return empty to skip this keyword and let other validators handle it
        if (!string.IsNullOrEmpty(targetUri.Fragment) && targetUri.Fragment != "#")
        {
            return $"// External $ref with fragment not supported in compiled mode: {refValue}";
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

        var e = context.ElementVariable;

        // Generate null check - if external ref wasn't initialized (no registry), validation fails
        return $"""
            // External $ref: {refValue}
            if ({fieldName} == null || !{fieldName}.IsValid({e})) return false;
            """;
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

    public IEnumerable<StaticFieldInfo> GetStaticFields(CodeGenerationContext context)
    {
        return [];
    }
}
