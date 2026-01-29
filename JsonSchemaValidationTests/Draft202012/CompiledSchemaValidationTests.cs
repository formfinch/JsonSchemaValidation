// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Nodes;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Compiler;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using FormFinch.JsonSchemaValidationTests.Common;
using FormFinch.JsonSchemaValidationTests.TestCases;
using Xunit.Sdk;

namespace FormFinch.JsonSchemaValidationTests.Draft202012
{
    /// <summary>
    /// Shared fixture for compiled schema validation tests.
    /// This fixture is created once and shared across all tests, avoiding
    /// expensive re-initialization of the registry and remote schema loading.
    /// </summary>
    public class CompiledSchemaValidationFixture : IDisposable
    {
        internal CompiledValidatorRegistry Registry { get; }
        internal RuntimeValidatorFactory Factory { get; }

        public CompiledSchemaValidationFixture()
        {
            Registry = CreateRegistryWithMetaschemas();
            Factory = new RuntimeValidatorFactory(Registry, forceAnnotationTracking: true);
        }

        public void Dispose()
        {
            Factory.Dispose();
        }

        private static CompiledValidatorRegistry CreateRegistryWithMetaschemas()
        {
            var registry = new CompiledValidatorRegistry();

            var metaschemas = CompiledMetaschemas.GetAll();

            // First pass: register all metaschemas so they can be resolved by external $ref
            foreach (var metaschema in metaschemas)
            {
                try
                {
                    registry.Register(metaschema);
                }
                catch
                {
                    // Ignore registration errors
                }
            }

            // Two-phase initialization: first register all subschemas, then initialize
            // Phase 1: Register subschemas from all metaschemas
            foreach (var metaschema in metaschemas)
            {
                try
                {
                    if (metaschema is IRegistryAwareCompiledValidator registryAware)
                    {
                        registryAware.RegisterSubschemas(registry);
                    }
                }
                catch
                {
                    // Ignore registration errors
                }
            }

            // Load remote schemas for test suite compatibility
            LoadRemoteSchemas(registry);

            // Note: RegisterVocabularyFragments is no longer needed - metaschemas now register
            // their own $defs subschemas via RegisterSubschemas()

            // Phase 2: Initialize registry-aware validators after all subschemas are registered
            foreach (var metaschema in metaschemas)
            {
                try
                {
                    if (metaschema is IRegistryAwareCompiledValidator registryAware)
                    {
                        registryAware.Initialize(registry);
                    }
                }
                catch
                {
                    // Ignore initialization errors - some refs may not be resolvable
                }
            }

            return registry;
        }

        private static void RegisterVocabularyFragments(CompiledValidatorRegistry registry, RuntimeValidatorFactory factory)
        {
            // Register specific $defs fragments from vocabulary schemas that are referenced by the metaschema
            var fragmentSchemas = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // meta/validation#/$defs/stringArray - referenced by dependencies compatibility
                ["https://json-schema.org/draft/2020-12/meta/validation#/$defs/stringArray"] =
                    """{ "type": "array", "items": { "type": "string" }, "default": [] }""",

                // meta/core#/$defs/anchorString - referenced by $recursiveAnchor compatibility
                ["https://json-schema.org/draft/2020-12/meta/core#/$defs/anchorString"] =
                    """{ "type": "string", "pattern": "^[A-Za-z_][-A-Za-z0-9._]*$" }""",

                // meta/core#/$defs/uriReferenceString - referenced by $recursiveRef compatibility
                ["https://json-schema.org/draft/2020-12/meta/core#/$defs/uriReferenceString"] =
                    """{ "type": "string", "format": "uri-reference" }""",
            };

            foreach (var (uri, schemaJson) in fragmentSchemas)
            {
                try
                {
                    var validator = factory.Compile(schemaJson);
                    registry.RegisterForUri(new Uri(uri), validator);
                }
                catch
                {
                    // Ignore errors compiling fragment schemas
                }
            }
        }

        private static void LoadRemoteSchemas(CompiledValidatorRegistry registry)
        {
            var remotesPath = @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes";
            if (!Directory.Exists(remotesPath)) return;

            // Collect all remote schema files first (including subschemas with fragments)
            var pendingSchemas = new List<(Uri SchemaUri, string Content)>();

            // Collect draft2020-12 remotes
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2020-12"), "http://localhost:1234/draft2020-12/");

            // Collect draft2019-09 remotes
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2019-09"), "http://localhost:1234/draft2019-09/");

            // Collect root-level remotes
            CollectRemotesFromPath(pendingSchemas, remotesPath, "http://localhost:1234/", topLevelOnly: true);

            if (pendingSchemas.Count == 0) return;

            // Use multi-pass compilation to handle dependencies
            // Each pass attempts to compile schemas that failed in previous passes
            using var factory = new RuntimeValidatorFactory(registry, forceAnnotationTracking: true);
            var maxPasses = 10; // Prevent infinite loops

            for (int pass = 0; pass < maxPasses && pendingSchemas.Count > 0; pass++)
            {
                var stillPending = new List<(Uri SchemaUri, string Content)>();

                foreach (var (schemaUri, content) in pendingSchemas)
                {
                    try
                    {
                        var validator = factory.Compile(content);
                        registry.RegisterForUri(schemaUri, validator);

                        // Also register by $id if present
                        try
                        {
                            registry.Register(validator);
                        }
                        catch
                        {
                            // Ignore if no $id or already registered
                        }
                    }
                    catch
                    {
                        // Failed to compile - probably has unresolved external refs
                        // Add to retry list for next pass
                        stillPending.Add((schemaUri, content));
                    }
                }

                // If no progress was made (same schemas still pending), break
                if (stillPending.Count == pendingSchemas.Count)
                {
                    break;
                }

                pendingSchemas = stillPending;
            }
        }

        private static void CollectRemotesFromPath(List<(Uri SchemaUri, string Content)> schemas, string path, string baseUrl, bool topLevelOnly = false)
        {
            if (!Directory.Exists(path)) return;

            var searchOption = topLevelOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
            foreach (var file in Directory.GetFiles(path, "*.json", searchOption))
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var relativePath = Path.GetRelativePath(path, file).Replace("\\", "/");
                    var schemaUri = new Uri(baseUrl + relativePath);

                    // Inject $id if not present, so fragment subschemas are registered correctly
                    content = InjectIdIfMissing(content, schemaUri.AbsoluteUri);

                    schemas.Add((schemaUri, content));

                    // Extract and register self-contained subschemas (those without internal $refs)
                    // Subschemas with $ref can't be compiled standalone as the ref can't be resolved
                    ExtractSelfContainedSubschemas(schemas, schemaUri, content);
                }
                catch
                {
                    // Ignore errors reading files
                }
            }
        }

        private static string InjectIdIfMissing(string content, string id)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    return content;
                }

                // If $id already present, return unchanged
                if (root.TryGetProperty("$id", out _))
                {
                    return content;
                }

                // Inject $id after the opening brace
                var firstBrace = content.IndexOf('{');
                if (firstBrace < 0)
                {
                    return content;
                }

                // Find position after any $schema declaration
                var insertPos = firstBrace + 1;
                if (root.TryGetProperty("$schema", out _))
                {
                    // Find the end of $schema property to insert after it
                    var schemaMatch = System.Text.RegularExpressions.Regex.Match(
                        content[(firstBrace + 1)..],
                        @"""?\$schema""?\s*:\s*(""[^""]*""|'[^']*')\s*,?");
                    if (schemaMatch.Success)
                    {
                        insertPos = firstBrace + 1 + schemaMatch.Index + schemaMatch.Length;
                    }
                }

                var injection = $"\n    \"$id\": \"{id}\",";
                return content.Insert(insertPos, injection);
            }
            catch
            {
                return content;
            }
        }

        /// <summary>
        /// Extract self-contained subschemas that don't have internal $ref/$dynamicRef.
        /// Subschemas with references can't be compiled standalone.
        /// </summary>
        private static void ExtractSelfContainedSubschemas(List<(Uri SchemaUri, string Content)> schemas, Uri baseUri, string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object) return;

                // Extract $defs subschemas that are self-contained
                if (root.TryGetProperty("$defs", out var defs) && defs.ValueKind == JsonValueKind.Object)
                {
                    foreach (var def in defs.EnumerateObject())
                    {
                        var subschemaContent = def.Value.GetRawText();
                        // Only extract if no internal $ref or $dynamicRef
                        if (!subschemaContent.Contains("\"$ref\"") && !subschemaContent.Contains("\"$dynamicRef\""))
                        {
                            var fragmentUri = new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#/$defs/{def.Name}");
                            schemas.Add((fragmentUri, subschemaContent));
                        }
                    }
                }

                // Extract anchors that are self-contained
                ExtractSelfContainedAnchors(schemas, baseUri, root);
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        /// <summary>
        /// Extract anchors ($anchor) from self-contained subschemas.
        /// </summary>
        private static void ExtractSelfContainedAnchors(List<(Uri SchemaUri, string Content)> schemas, Uri baseUri, JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return;

            var content = element.GetRawText();

            // Only extract if no internal $ref or $dynamicRef
            if (!content.Contains("\"$ref\"") && !content.Contains("\"$dynamicRef\""))
            {
                // Check for $anchor
                if (element.TryGetProperty("$anchor", out var anchor) && anchor.ValueKind == JsonValueKind.String)
                {
                    var anchorName = anchor.GetString();
                    if (!string.IsNullOrEmpty(anchorName))
                    {
                        var anchorUri = new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#{anchorName}");
                        if (!schemas.Any(s => s.SchemaUri.AbsoluteUri == anchorUri.AbsoluteUri))
                        {
                            schemas.Add((anchorUri, content));
                        }
                    }
                }

                // Check for $dynamicAnchor
                if (element.TryGetProperty("$dynamicAnchor", out var dynamicAnchor) && dynamicAnchor.ValueKind == JsonValueKind.String)
                {
                    var anchorName = dynamicAnchor.GetString();
                    if (!string.IsNullOrEmpty(anchorName))
                    {
                        var anchorUri = new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#{anchorName}");
                        if (!schemas.Any(s => s.SchemaUri.AbsoluteUri == anchorUri.AbsoluteUri))
                        {
                            schemas.Add((anchorUri, content));
                        }
                    }
                }
            }

            // Recurse into nested objects to find more anchors
            if (element.TryGetProperty("$defs", out var defs) && defs.ValueKind == JsonValueKind.Object)
            {
                foreach (var def in defs.EnumerateObject())
                {
                    ExtractSelfContainedAnchors(schemas, baseUri, def.Value);
                }
            }
        }

    }

    [Trait("Draft", "2020-12")]
    [Trait("Validator", "Compiled")]
    public class CompiledSchemaValidationTests : IClassFixture<CompiledSchemaValidationFixture>
    {
        private readonly CompiledValidatorRegistry _registry;
        private readonly RuntimeValidatorFactory _factory;

        public CompiledSchemaValidationTests(CompiledSchemaValidationFixture fixture)
        {
            _registry = fixture.Registry;
            _factory = fixture.Factory;
        }

        [Theory]
        [MemberData(nameof(GetDraft202012Tests))]
        public void Draft202012CompiledTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [Theory]
        [MemberData(nameof(GetDraft202012FormatAssertionTests))]
        public void Draft202012CompiledFormatAssertionTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft202012KnownLimitationTests))]
        public void Draft202012CompiledKnownLimitationTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft202012FormatAssertionKnownLimitationTests))]
        public void Draft202012CompiledFormatAssertionKnownLimitationTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        private void RunTestCase(TestCase testCase)
        {
            // Compile the schema
            var schemaJson = testCase.Schema.GetRawText();
            ICompiledValidator compiledValidator;

            try
            {
                compiledValidator = _factory.Compile(schemaJson);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to compile schema for test '{testCase.Description}': {ex.Message}", ex);
            }

            // Register the validator so it can be referenced by other schemas
            try
            {
                _registry.Register(compiledValidator);
            }
            catch
            {
                // Ignore registration errors (e.g., schema has no $id)
            }

            foreach (var test in testCase.Tests)
            {
                var prpDescription = test.GetProperty("description");
                string testDescription = prpDescription.GetString()!;

                var testData = test.GetProperty("data");
                var expectedResult = test.GetProperty("valid").GetBoolean();
                var actualResult = compiledValidator.IsValid(testData);

                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    Assert.Equal(expectedResult, actualResult);
                }
                else
                {
                    try
                    {
                        Assert.Equal(expectedResult, actualResult);
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine($"\"{testDescription}\"");
                    }
                }
            }
        }

        public static IEnumerable<object[]> GetDraft202012Tests()
            => GetAllDraft202012Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft202012KnownLimitationTests()
            => GetAllDraft202012Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft202012Tests()
            => new TestCaseLoader(new string[] {
                ///* implemented keyword tests */
                "additionalProperties",
                "allOf",
                "anchor",
                "anyOf",
                "boolean_schema",
                "const",
                "contains",
                "content",
                "default",
                "defs",
                "dependentRequired",
                "dependentSchemas",
                "dynamicRef",
                "enum",
                "exclusiveMaximum",
                "exclusiveMinimum",
                // Note: "format" is excluded - tests annotation-only behavior, but compiled validators always validate format
                "id",
                "if-then-else",
                "infinite-loop-detection",
                "items",
                "maxContains",
                "maximum",
                "maxItems",
                "maxLength",
                "maxProperties",
                "minContains",
                "minimum",
                "minItems",
                "minLength",
                "minProperties",
                "multipleOf",
                "not",
                "oneOf",
                "pattern",
                "patternProperties",
                "prefixItems",
                "properties",
                "propertyNames",
                "ref",
                "refRemote",
                "required",
                "type",
                "unevaluatedItems",
                "unevaluatedProperties",
                "uniqueItems",
                "unknownKeyword",
                "vocabulary",

                @"\optional\anchor",
                @"\optional\bignum",
                @"\optional\cross-draft",
                @"\optional\dependencies-compatibility",
                @"\optional\dynamicRef",
                @"\optional\float-overflow",
                @"\optional\format-assertion",                  // Requires vocabulary support
                @"\optional\id",
                @"\optional\no-schema",
                @"\optional\non-bmp-regex",
                @"\optional\refOfUnknownKeyword",
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2020-12");

        /// <summary>
        /// Returns test cases for optional format validation tests.
        /// These tests run with format assertion enabled (compiled validators always validate formats).
        /// </summary>
        public static IEnumerable<object[]> GetDraft202012FormatAssertionTests()
            => GetAllDraft202012FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft202012FormatAssertionKnownLimitationTests()
            => GetAllDraft202012FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft202012FormatAssertionTests()
            => new TestCaseLoader(new string[] {
                @"\optional\ecmascript-regex",                  // Requires format:regex validation for metaschema
                @"\optional\format\date-time",
                @"\optional\format\date",
                @"\optional\format\duration",
                @"\optional\format\email",
                @"\optional\format\hostname",
                @"\optional\format\idn-email",
                @"\optional\format\idn-hostname",
                @"\optional\format\ipv4",
                @"\optional\format\ipv6",
                @"\optional\format\iri",
                @"\optional\format\iri-reference",
                @"\optional\format\json-pointer",
                @"\optional\format\regex",
                @"\optional\format\relative-json-pointer",
                @"\optional\format\time",
                @"\optional\format\unknown",
                @"\optional\format\uri",
                @"\optional\format\uri-reference",
                @"\optional\format\uri-template",
                @"\optional\format\uuid",
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2020-12");

        /// <summary>
        /// Returns a skip reason if the test case should be skipped, null otherwise.
        /// Uses standardized skip reason constants from <see cref="SkipReasons"/>.
        /// </summary>
        private static string? GetSkipReason(string testCaseDescription)
        {
            // Infinite loop detection tests now work correctly

            // Complex $dynamicRef scenarios (TASK-048)
            // Tests verified 2026-01-28:
            // - "multiple dynamic paths" PASSES with resource-level anchor collection
            // - "A $dynamicRef that initially resolves..." PASSES with cross-resource parsing
            // - "after leaving a dynamic scope" PASSES with cross-resource parsing
            // - "$dynamicRef avoids the root" PASSES with resource-level scope pushing
            // - "$dynamicRef skips over intermediate" PASSES with resource-level scope pushing
            // - "strict-tree schema..." REGRESSION - path not propagated across schema boundaries
            //   when external schema (tree.json) without annotation tracking calls back to
            //   schema with annotation tracking (strict-tree.json) via $dynamicRef
            var complexDynamicRefTests = new[]
            {
                "strict-tree schema, guards against misspelled properties"
            };

            var allowComplexDynamicRef = string.Equals(
                Environment.GetEnvironmentVariable("FF_ALLOW_COMPLEX_DYNAMICREF"),
                "1",
                StringComparison.Ordinal);
            if (!allowComplexDynamicRef &&
                complexDynamicRefTests.Any(t => testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.ComplexDynamicRefNotSupported;
            }

            // Vocabulary-based validation
            if (testCaseDescription == "schema that uses custom metaschema with with no validation vocabulary" ||
                testCaseDescription.StartsWith("schema that uses custom metaschema with with no validation vocabulary", StringComparison.Ordinal))
            {
                return SkipReasons.VocabularyBased;
            }

            // Cross-draft compatibility
            if (testCaseDescription == "refs to historic drafts are processed as historic drafts" ||
                testCaseDescription.StartsWith("refs to historic drafts are processed as historic drafts", StringComparison.Ordinal))
            {
                return SkipReasons.CrossDraft;
            }

            return null;
        }
    }
}
