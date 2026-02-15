// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using FormFinch.JsonSchemaValidation.Compiler;
using FormFinch.JsonSchemaValidationTests.Common;
using FormFinch.JsonSchemaValidationTests.TestCases;
using Xunit.Sdk;

namespace FormFinch.JsonSchemaValidationTests.Draft201909
{
    /// <summary>
    /// Shared fixture for Draft 2019-09 compiled schema validation tests.
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
            Factory = new RuntimeValidatorFactory(Registry, forceAnnotationTracking: true, defaultDraft: SchemaDraft.Draft201909);
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

        private static void LoadRemoteSchemas(CompiledValidatorRegistry registry)
        {
            var remotesPath = "../../../../submodules/JSON-Schema-Test-Suite/remotes";
            if (!Directory.Exists(remotesPath)) return;

            // Collect all remote schema files first (including subschemas with fragments)
            var pendingSchemas = new List<(Uri SchemaUri, string Content)>();

            // Collect draft2019-09 remotes
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2019-09"), "http://localhost:1234/draft2019-09/");

            // Collect draft2020-12 remotes (for cross-draft tests)
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2020-12"), "http://localhost:1234/draft2020-12/");

            // Collect draft7 remotes (for cross-draft tests)
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft7"), "http://localhost:1234/draft7/");

            // Collect root-level remotes
            CollectRemotesFromPath(pendingSchemas, remotesPath, "http://localhost:1234/", topLevelOnly: true);

            if (pendingSchemas.Count == 0) return;

            // Use multi-pass compilation to handle dependencies
            using var factory = new RuntimeValidatorFactory(registry, forceAnnotationTracking: true, defaultDraft: SchemaDraft.Draft201909);
            var maxPasses = 10;

            for (int pass = 0; pass < maxPasses && pendingSchemas.Count > 0; pass++)
            {
                var stillPending = new List<(Uri SchemaUri, string Content)>();

                foreach (var (schemaUri, content) in pendingSchemas)
                {
                    try
                    {
                        var validator = factory.Compile(content);
                        registry.RegisterForUri(schemaUri, validator);

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
                        stillPending.Add((schemaUri, content));
                    }
                }

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

                    // Extract anchors from schema for anchor-based references
                    ExtractAnchors(schemas, schemaUri, content);
                }
                catch
                {
                    // Ignore errors reading files
                }
            }
        }

        private static void ExtractAnchors(List<(Uri SchemaUri, string Content)> schemas, Uri baseUri, string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object) return;

                ExtractAnchorsRecursive(schemas, baseUri, root);
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        private static void ExtractAnchorsRecursive(List<(Uri SchemaUri, string Content)> schemas, Uri baseUri, JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return;

            var elementContent = element.GetRawText();

            // Check for $anchor
            if (element.TryGetProperty("$anchor", out var anchor) && anchor.ValueKind == JsonValueKind.String)
            {
                var anchorName = anchor.GetString();
                if (!string.IsNullOrEmpty(anchorName))
                {
                    // Only extract if no internal $ref (can't compile standalone)
                    if (!elementContent.Contains("\"$ref\"") && !elementContent.Contains("\"$recursiveRef\""))
                    {
                        var anchorUri = new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#{anchorName}");
                        if (!schemas.Any(s => s.SchemaUri.AbsoluteUri == anchorUri.AbsoluteUri))
                        {
                            schemas.Add((anchorUri, elementContent));
                        }
                    }
                }
            }

            // Recurse into $defs
            if (element.TryGetProperty("$defs", out var defs) && defs.ValueKind == JsonValueKind.Object)
            {
                foreach (var def in defs.EnumerateObject())
                {
                    ExtractAnchorsRecursive(schemas, baseUri, def.Value);
                }
            }

            // Recurse into definitions (Draft 7 style)
            if (element.TryGetProperty("definitions", out var definitions) && definitions.ValueKind == JsonValueKind.Object)
            {
                foreach (var def in definitions.EnumerateObject())
                {
                    ExtractAnchorsRecursive(schemas, baseUri, def.Value);
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
    }

    [Trait("Draft", "2019-09")]
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
        [MemberData(nameof(GetDraft201909Tests))]
        public void Draft201909CompiledTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [Theory]
        [MemberData(nameof(GetDraft201909FormatAssertionTests))]
        public void Draft201909CompiledFormatAssertionTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft201909KnownLimitationTests))]
        public void Draft201909CompiledKnownLimitationTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft201909FormatAssertionKnownLimitationTests))]
        public void Draft201909CompiledFormatAssertionKnownLimitationTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        private void RunTestCase(TestCase testCase)
        {
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

        public static IEnumerable<object[]> GetDraft201909Tests()
            => GetAllDraft201909Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft201909KnownLimitationTests()
            => GetAllDraft201909Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft201909Tests()
            => new TestCaseLoader(new string[] {
                "additionalItems",
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
                "properties",
                "propertyNames",
                "recursiveRef",
                "ref",
                "refRemote",
                "required",
                "type",
                "unevaluatedItems",
                "unevaluatedProperties",
                "uniqueItems",
                "unknownKeyword",
                "vocabulary",

                "/optional/anchor",
                "/optional/bignum",
                "/optional/cross-draft",
                "/optional/dependencies-compatibility",
                "/optional/float-overflow",
                "/optional/format-assertion",
                "/optional/id",
                "/optional/no-schema",
                "/optional/non-bmp-regex",
                "/optional/refOfUnknownKeyword",
            }).LoadTestCases("../../../../submodules/JSON-Schema-Test-Suite/tests/draft2019-09");

        public static IEnumerable<object[]> GetDraft201909FormatAssertionTests()
            => GetAllDraft201909FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft201909FormatAssertionKnownLimitationTests()
            => GetAllDraft201909FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft201909FormatAssertionTests()
            => new TestCaseLoader(new string[] {
                "/optional/ecmascript-regex",
                "/optional/format/date-time",
                "/optional/format/date",
                "/optional/format/duration",
                "/optional/format/email",
                "/optional/format/hostname",
                "/optional/format/idn-email",
                "/optional/format/idn-hostname",
                "/optional/format/ipv4",
                "/optional/format/ipv6",
                "/optional/format/iri",
                "/optional/format/iri-reference",
                "/optional/format/json-pointer",
                "/optional/format/regex",
                "/optional/format/relative-json-pointer",
                "/optional/format/time",
                "/optional/format/unknown",
                "/optional/format/uri",
                "/optional/format/uri-reference",
                "/optional/format/uri-template",
                "/optional/format/uuid",
            }).LoadTestCases("../../../../submodules/JSON-Schema-Test-Suite/tests/draft2019-09");

        /// <summary>
        /// Returns a skip reason if the test case should be skipped, null otherwise.
        /// Uses standardized skip reason constants from <see cref="SkipReasons"/>.
        /// </summary>
        private static string? GetSkipReason(string testCaseDescription)
        {
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
