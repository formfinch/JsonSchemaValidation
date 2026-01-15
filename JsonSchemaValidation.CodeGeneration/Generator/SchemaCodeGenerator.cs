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
            new TypeCodeGenerator(),
            new RequiredCodeGenerator(),
            new EnumCodeGenerator(),
            new ConstCodeGenerator(),
            new StringConstraintsCodeGenerator(),
            new NumericConstraintsCodeGenerator(),
            new ArrayConstraintsCodeGenerator(),
            new PrefixItemsCodeGenerator(),
            new ItemsCodeGenerator(),
            new ContainsCodeGenerator(),
            new PropertiesCodeGenerator(),
            new PatternPropertiesCodeGenerator(),
            new AdditionalPropertiesCodeGenerator(),
            new PropertyNamesCodeGenerator(),
            new DependentRequiredCodeGenerator(),
            new DependentSchemasCodeGenerator(),
            new AllOfCodeGenerator(),
            new AnyOfCodeGenerator(),
            new OneOfCodeGenerator(),
            new NotCodeGenerator(),
            new IfThenElseCodeGenerator(),
            new PatternCodeGenerator()
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

            // Extract all unique subschemas
            var uniqueSchemas = _extractor.ExtractUniqueSubschemas(schema);
            var rootHash = SchemaHasher.ComputeHash(schema);

            // Collect all static fields
            var allStaticFields = new List<StaticFieldInfo>();

            // Generate validation methods for each unique subschema
            var methods = new StringBuilder();
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                var methodCode = GenerateValidationMethod(subschemaInfo, uniqueSchemas);
                methods.AppendLine(methodCode);
                methods.AppendLine();
            }

            // Collect static fields from all subschemas
            foreach (var (hash, subschemaInfo) in uniqueSchemas)
            {
                var context = CreateContext(subschemaInfo, uniqueSchemas);
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

            // Generate the full class
            var code = GenerateClass(
                namespaceName,
                resolvedClassName,
                schemaUri,
                rootHash,
                allStaticFields,
                methods.ToString());

            var fileName = $"{resolvedClassName}.cs";
            return GenerationResult.Succeeded(code, fileName);
        }
        catch (Exception ex)
        {
            return GenerationResult.Failed($"Code generation failed: {ex.Message}");
        }
    }

    private string GenerateValidationMethod(SubschemaInfo subschemaInfo, Dictionary<string, SubschemaInfo> allSchemas)
    {
        var context = CreateContext(subschemaInfo, allSchemas);
        var sb = new StringBuilder();

        sb.AppendLine($"    private static bool Validate_{subschemaInfo.Hash}(JsonElement e)");
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

    private CodeGenerationContext CreateContext(SubschemaInfo subschemaInfo, Dictionary<string, SubschemaInfo> allSchemas)
    {
        return new CodeGenerationContext
        {
            CurrentSchema = subschemaInfo.Schema,
            CurrentHash = subschemaInfo.Hash,
            GetSubschemaHash = element => SchemaHasher.ComputeHash(element),
            ResolveLocalRef = refValue => _extractor.ResolveLocalRef(refValue)
        };
    }

    private string GenerateClass(
        string namespaceName,
        string className,
        string? schemaUri,
        string rootHash,
        List<StaticFieldInfo> staticFields,
        string methods)
    {
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
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        // Class declaration
        sb.AppendLine($"    public sealed class {className} : ICompiledValidator");
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

        // IsValid entry point
        sb.AppendLine($"        public bool IsValid(JsonElement instance) => Validate_{rootHash}(instance);");
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
