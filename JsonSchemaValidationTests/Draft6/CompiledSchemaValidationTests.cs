// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.Compiler;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using FormFinch.JsonSchemaValidationTests.Common;
using FormFinch.JsonSchemaValidationTests.TestCases;
using Xunit.Sdk;

namespace FormFinch.JsonSchemaValidationTests.Draft6
{
    /// <summary>
    /// Shared fixture for Draft 6 compiled schema validation tests.
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
            Factory = new RuntimeValidatorFactory(Registry, forceAnnotationTracking: false, defaultDraft: SchemaDraft.Draft6);
        }

        public void Dispose()
        {
            Factory.Dispose();
        }

        private static CompiledValidatorRegistry CreateRegistryWithMetaschemas()
        {
            var registry = new CompiledValidatorRegistry();

            var metaschemas = CompiledMetaschemas.GetAll();

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

            LoadRemoteSchemas(registry);

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
            var remotesPath = @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes";
            if (!Directory.Exists(remotesPath)) return;

            var pendingSchemas = new List<(Uri SchemaUri, string Content)>();

            // Collect draft6 remotes
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft6"), "http://localhost:1234/draft6/");

            // Collect future draft remotes for cross-draft tests
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft7"), "http://localhost:1234/draft7/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2019-09"), "http://localhost:1234/draft2019-09/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2020-12"), "http://localhost:1234/draft2020-12/");

            // Collect root-level remotes (including subdirectories like baseUriChange/, nested/)
            CollectRemotesFromPath(pendingSchemas, remotesPath, "http://localhost:1234/");

            if (pendingSchemas.Count == 0) return;

            using var factory = new RuntimeValidatorFactory(registry, forceAnnotationTracking: false, defaultDraft: SchemaDraft.Draft6);
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

    [Trait("Draft", "6")]
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
        [MemberData(nameof(GetDraft6Tests))]
        public void Draft6CompiledTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [Theory]
        [MemberData(nameof(GetDraft6FormatAssertionTests))]
        public void Draft6CompiledFormatAssertionTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft6KnownLimitationTests))]
        public void Draft6CompiledKnownLimitationTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft6FormatAssertionKnownLimitationTests))]
        public void Draft6CompiledFormatAssertionKnownLimitationTests(TestCase testCase)
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

        public static IEnumerable<object[]> GetDraft6Tests()
            => GetAllDraft6Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft6KnownLimitationTests()
            => GetAllDraft6Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft6Tests()
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

                @"\optional\bignum",
                @"\optional\float-overflow",
                @"\optional\non-bmp-regex",
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft6");

        public static IEnumerable<object[]> GetDraft6FormatAssertionTests()
            => GetAllDraft6FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft6FormatAssertionKnownLimitationTests()
            => GetAllDraft6FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft6FormatAssertionTests()
            => new TestCaseLoader(new string[] {
                @"\optional\ecmascript-regex",
                @"\optional\id",
                @"\optional\unknownKeyword",
                @"\optional\format\date-time",
                @"\optional\format\email",
                @"\optional\format\hostname",
                @"\optional\format\ipv4",
                @"\optional\format\ipv6",
                @"\optional\format\json-pointer",
                @"\optional\format\unknown",
                @"\optional\format\uri",
                @"\optional\format\uri-reference",
                @"\optional\format\uri-template",
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft6");

        /// <summary>
        /// Returns a skip reason if the test case should be skipped, null otherwise.
        /// Uses standardized skip reason constants from <see cref="SkipReasons"/>.
        /// </summary>
        private static string? GetSkipReason(string testCaseDescription)
        {
            // Draft 6 uses different $ref semantics - $ref overrides sibling keywords
            if (testCaseDescription == "ref overrides any sibling keywords" ||
                testCaseDescription.StartsWith("ref overrides any sibling keywords", StringComparison.Ordinal))
            {
                return SkipReasons.RefOverrideSemantics;
            }

            // Draft 6 uses $id: "#fragment" for location-independent identifiers (anchors)
            if (testCaseDescription == "$ref prevents a sibling $id from changing the base uri" ||
                testCaseDescription.StartsWith("$ref prevents a sibling $id from changing the base uri", StringComparison.Ordinal))
            {
                return SkipReasons.RefOverrideSemantics;
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
