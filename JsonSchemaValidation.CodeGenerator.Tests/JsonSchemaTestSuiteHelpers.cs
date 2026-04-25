// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Nodes;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.Schema;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

internal static class JsonSchemaTestSuiteHelpers
{
    public static string? FindTestSuitePath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir != null; i++)
        {
            var candidate = Path.Combine(dir, "submodules", "JSON-Schema-Test-Suite");
            if (Directory.Exists(Path.Combine(candidate, "tests"))) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    public static void CollectRemotesFromPath(
        List<(Uri SchemaUri, string Content)> schemas,
        string path,
        string baseUrl,
        bool topLevelOnly = false)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var searchOption = topLevelOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
        foreach (var file in Directory.GetFiles(path, "*.json", searchOption))
        {
            try
            {
                var content = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(path, file).Replace("\\", "/");
                var schemaUri = new Uri(baseUrl + relativePath);
                content = InjectIdIfMissing(content, schemaUri.AbsoluteUri);
                schemas.Add((schemaUri, content));
                ExtractSelfContainedSubschemas(schemas, schemaUri, content);
            }
            catch
            {
                // Ignore malformed or unreadable remotes here; suite cases that
                // depend on them still surface failures if needed.
            }
        }
    }

    public static void CollectBundledDraft202012Schemas(List<(Uri SchemaUri, string Content)> schemas)
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot == null)
        {
            return;
        }

        var dataPath = Path.Combine(repoRoot, "JsonSchemaValidation", "Draft202012", "Data");
        if (!Directory.Exists(dataPath))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(dataPath, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var content = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty("$id", out var idElement) ||
                    idElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrEmpty(idElement.GetString()) ||
                    !Uri.TryCreate(idElement.GetString(), UriKind.Absolute, out var schemaUri))
                {
                    continue;
                }

                schemas.Add((schemaUri, content));
            }
            catch
            {
                // Best effort preload; suite failures remain visible if this misses something.
            }
        }
    }

    public static IEnumerable<JsTestSuiteRunner.TestCase> EnumerateKeywordCases(
        string draftFolder,
        IEnumerable<string> keywords,
        SchemaDraft draft,
        bool formatAssertionEnabled)
    {
        var suitePath = FindTestSuitePath();
        if (suitePath == null)
        {
            yield return JsTestSuiteRunner.TestCase.MissingSuite(draft, formatAssertionEnabled);
            yield break;
        }

        var root = Path.Combine(suitePath, "tests", draftFolder);
        if (!Directory.Exists(root))
        {
            yield break;
        }

        var wanted = keywords
            .Select(NormalizeKeyword)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(root, "*.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace("\\", "/");
            var relativeNoExt = NormalizeKeyword(relative);
            if (!wanted.Contains(relativeNoExt))
            {
                continue;
            }

            var json = File.ReadAllText(file);
            using var doc = JsonDocument.Parse(json);
            foreach (var group in doc.RootElement.EnumerateArray())
            {
                var groupDesc = group.GetProperty("description").GetString() ?? string.Empty;
                var schema = group.GetProperty("schema").GetRawText();
                foreach (var test in group.GetProperty("tests").EnumerateArray())
                {
                    yield return new JsTestSuiteRunner.TestCase(
                        draft,
                        relative,
                        relativeNoExt,
                        groupDesc,
                        test.GetProperty("description").GetString() ?? string.Empty,
                        schema,
                        test.GetProperty("data").GetRawText(),
                        test.GetProperty("valid").GetBoolean(),
                        formatAssertionEnabled);
                }
            }
        }
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir != null; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "JsonSchemaValidation")) &&
                Directory.Exists(Path.Combine(dir, "JsonSchemaValidation.CodeGenerator.Tests")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private static string InjectIdIfMissing(string content, string id)
    {
        try
        {
            var node = JsonNode.Parse(content);
            if (node is not JsonObject obj || obj.ContainsKey("$id"))
            {
                return content;
            }

            var reordered = new JsonObject();
            if (obj.ContainsKey("$schema"))
            {
                var schemaNode = obj["$schema"];
                obj.Remove("$schema");
                reordered["$schema"] = schemaNode?.DeepClone();
            }
            reordered["$id"] = id;
            foreach (var (key, value) in obj.ToList())
            {
                reordered[key] = value?.DeepClone();
            }
            return reordered.ToJsonString();
        }
        catch
        {
            return content;
        }
    }

    private static void ExtractSelfContainedSubschemas(
        List<(Uri SchemaUri, string Content)> schemas,
        Uri baseUri,
        string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (root.TryGetProperty("$defs", out var defs) && defs.ValueKind == JsonValueKind.Object)
            {
                foreach (var def in defs.EnumerateObject())
                {
                    var subschemaContent = def.Value.GetRawText();
                    if (!subschemaContent.Contains("\"$ref\"", StringComparison.Ordinal) &&
                        !subschemaContent.Contains("\"$dynamicRef\"", StringComparison.Ordinal))
                    {
                        schemas.Add((new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#/$defs/{def.Name}"), subschemaContent));
                    }
                }
            }

            ExtractSelfContainedAnchors(schemas, baseUri, root);
        }
        catch
        {
            // Ignore parse failures during best-effort fragment extraction.
        }
    }

    private static void ExtractSelfContainedAnchors(
        List<(Uri SchemaUri, string Content)> schemas,
        Uri baseUri,
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var content = element.GetRawText();
        if (!content.Contains("\"$ref\"", StringComparison.Ordinal) &&
            !content.Contains("\"$dynamicRef\"", StringComparison.Ordinal))
        {
            if (element.TryGetProperty("$anchor", out var anchor) &&
                anchor.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(anchor.GetString()))
            {
                schemas.Add((new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#{anchor.GetString()}"), content));
            }

            if (element.TryGetProperty("$dynamicAnchor", out var dynamicAnchor) &&
                dynamicAnchor.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(dynamicAnchor.GetString()))
            {
                schemas.Add((new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#{dynamicAnchor.GetString()}"), content));
            }
        }

        if (element.TryGetProperty("$defs", out var defs) && defs.ValueKind == JsonValueKind.Object)
        {
            foreach (var def in defs.EnumerateObject())
            {
                ExtractSelfContainedAnchors(schemas, baseUri, def.Value);
            }
        }
    }

    private static string NormalizeKeyword(string keyword)
    {
        var normalized = keyword.Replace("\\", "/").TrimStart('/');
        return normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^5]
            : normalized;
    }
}
