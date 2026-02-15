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

namespace FormFinch.JsonSchemaValidationTests.Draft7
{
    /// <summary>
    /// Shared fixture for Draft 7 compiled schema validation tests.
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
            Factory = new RuntimeValidatorFactory(Registry, forceAnnotationTracking: false, defaultDraft: SchemaDraft.Draft7);
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

            // Initialize registry-aware validators after all subschemas are registered
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
                    // Ignore initialization errors
                }
            }

            return registry;
        }

        private static void LoadRemoteSchemas(CompiledValidatorRegistry registry)
        {
            var remotesPath = "../../../../submodules/JSON-Schema-Test-Suite/remotes";
            if (!Directory.Exists(remotesPath)) return;

            var pendingSchemas = new List<(Uri SchemaUri, string Content)>();

            // Collect draft7 remotes
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft7"), "http://localhost:1234/draft7/");

            // Collect future draft remotes for cross-draft tests
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2019-09"), "http://localhost:1234/draft2019-09/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2020-12"), "http://localhost:1234/draft2020-12/");

            // Collect root-level remotes (including subdirectories like baseUriChange/, nested/)
            CollectRemotesFromPath(pendingSchemas, remotesPath, "http://localhost:1234/");

            if (pendingSchemas.Count == 0) return;

            using var factory = new RuntimeValidatorFactory(registry, forceAnnotationTracking: false, defaultDraft: SchemaDraft.Draft7);
            var maxPasses = 10;
            var compiledValidators = new List<ICompiledValidator>();

            for (int pass = 0; pass < maxPasses && pendingSchemas.Count > 0; pass++)
            {
                var stillPending = new List<(Uri SchemaUri, string Content)>();

                foreach (var (schemaUri, content) in pendingSchemas)
                {
                    try
                    {
                        var validator = factory.Compile(content);
                        registry.RegisterForUri(schemaUri, validator);
                        compiledValidators.Add(validator);

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

            // Two-phase initialization: first register all subschemas (fragment URIs)
            foreach (var validator in compiledValidators)
            {
                try
                {
                    if (validator is IRegistryAwareCompiledValidator registryAware)
                    {
                        registryAware.RegisterSubschemas(registry);
                    }
                }
                catch
                {
                    // Ignore registration errors
                }
            }

            // Then initialize (resolve external refs)
            foreach (var validator in compiledValidators)
            {
                try
                {
                    if (validator is IRegistryAwareCompiledValidator registryAware)
                    {
                        registryAware.Initialize(registry);
                    }
                }
                catch
                {
                    // Ignore initialization errors
                }
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

                var injection = $"\n    \"$id\": \"{id}\",";
                return content.Insert(firstBrace + 1, injection);
            }
            catch
            {
                return content;
            }
        }
    }

    [Trait("Draft", "7")]
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
        [MemberData(nameof(GetDraft7Tests))]
        public void Draft7CompiledTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [Theory]
        [MemberData(nameof(GetDraft7FormatAssertionTests))]
        public void Draft7CompiledFormatAssertionTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [Theory]
        [MemberData(nameof(GetDraft7ContentAssertionTests))]
        public void Draft7CompiledContentAssertionTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft7KnownLimitationTests))]
        public void Draft7CompiledKnownLimitationTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft7FormatAssertionKnownLimitationTests))]
        public void Draft7CompiledFormatAssertionKnownLimitationTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft7ContentAssertionKnownLimitationTests))]
        public void Draft7CompiledContentAssertionKnownLimitationTests(TestCase testCase)
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

        public static IEnumerable<object[]> GetDraft7Tests()
            => GetAllDraft7Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft7KnownLimitationTests()
            => GetAllDraft7Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft7Tests()
            => new TestCaseLoader(new string[] {
                "additionalItems",
                "additionalProperties",
                "allOf",
                "anyOf",
                "boolean_schema",
                "const",
                "contains",
                "default",
                "definitions",
                "dependencies",
                "enum",
                "exclusiveMaximum",
                "exclusiveMinimum",
                // Note: "format" is excluded - tests annotation-only behavior
                "if-then-else",
                "infinite-loop-detection",
                "items",
                "maximum",
                "maxItems",
                "maxLength",
                "maxProperties",
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
                "ref",
                "refRemote",
                "required",
                "type",
                "uniqueItems",

                "/optional/bignum",
                "/optional/cross-draft",
                "/optional/float-overflow",
                "/optional/non-bmp-regex",
                "/optional/unknownKeyword",
            }).LoadTestCases("../../../../submodules/JSON-Schema-Test-Suite/tests/draft7");

        public static IEnumerable<object[]> GetDraft7FormatAssertionTests()
            => GetAllDraft7FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft7FormatAssertionKnownLimitationTests()
            => GetAllDraft7FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft7FormatAssertionTests()
            => new TestCaseLoader(new string[] {
                "/optional/ecmascript-regex",
                "/optional/format/date-time",
                "/optional/format/date",
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
            }).LoadTestCases("../../../../submodules/JSON-Schema-Test-Suite/tests/draft7");

        public static IEnumerable<object[]> GetDraft7ContentAssertionTests()
            => GetAllDraft7ContentAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft7ContentAssertionKnownLimitationTests()
            => GetAllDraft7ContentAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft7ContentAssertionTests()
            => new TestCaseLoader(new string[] {
                "/optional/content",
            }).LoadTestCases("../../../../submodules/JSON-Schema-Test-Suite/tests/draft7");

        /// <summary>
        /// Returns a skip reason if the test case should be skipped, null otherwise.
        /// Uses standardized skip reason constants from <see cref="SkipReasons"/>.
        /// </summary>
        private static string? GetSkipReason(string testCaseDescription)
        {
            // Cross-draft compatibility
            if (testCaseDescription == "refs to future drafts are processed as future drafts" ||
                testCaseDescription.StartsWith("refs to future drafts are processed as future drafts", StringComparison.Ordinal))
            {
                return SkipReasons.CrossDraft;
            }

            return null;
        }
    }
}
