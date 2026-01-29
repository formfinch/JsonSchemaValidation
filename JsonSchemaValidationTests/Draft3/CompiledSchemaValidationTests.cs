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

namespace FormFinch.JsonSchemaValidationTests.Draft3
{
    /// <summary>
    /// Shared fixture for Draft 3 compiled schema validation tests.
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
            Factory = new RuntimeValidatorFactory(Registry, forceAnnotationTracking: false, defaultDraft: SchemaDraft.Draft3);
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

            // Collect draft3 remotes
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft3"), "http://localhost:1234/draft3/");

            // Collect future draft remotes for cross-draft tests
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft4"), "http://localhost:1234/draft4/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft6"), "http://localhost:1234/draft6/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft7"), "http://localhost:1234/draft7/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2019-09"), "http://localhost:1234/draft2019-09/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2020-12"), "http://localhost:1234/draft2020-12/");

            // Collect root-level remotes (including subdirectories like baseUriChange/, nested/)
            CollectRemotesFromPath(pendingSchemas, remotesPath, "http://localhost:1234/");

            if (pendingSchemas.Count == 0) return;

            using var factory = new RuntimeValidatorFactory(registry, forceAnnotationTracking: false, defaultDraft: SchemaDraft.Draft3);
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
                            // Ignore if no id or already registered
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

                    // Inject id if not present, so fragment subschemas are registered correctly
                    // Draft 3 uses "id" instead of "$id"
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

                // If id already present, return unchanged (Draft 3 uses 'id', not '$id')
                if (root.TryGetProperty("id", out _))
                {
                    return content;
                }

                // Inject id after the opening brace
                var firstBrace = content.IndexOf('{');
                if (firstBrace < 0)
                {
                    return content;
                }

                var injection = $"\n    \"id\": \"{id}\",";
                return content.Insert(firstBrace + 1, injection);
            }
            catch
            {
                return content;
            }
        }
    }

    [Trait("Draft", "3")]
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
        [MemberData(nameof(GetDraft3Tests))]
        public void Draft3CompiledTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [Theory]
        [MemberData(nameof(GetDraft3FormatAssertionTests))]
        public void Draft3CompiledFormatAssertionTests(TestCase testCase)
        {
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft3KnownLimitationTests))]
        public void Draft3CompiledKnownLimitationTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [Trait("Category", "KnownLimitation")]
        [MemberData(nameof(GetDraft3FormatAssertionKnownLimitationTests))]
        public void Draft3CompiledFormatAssertionKnownLimitationTests(TestCase testCase)
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
                // Ignore registration errors (e.g., schema has no id)
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

        public static IEnumerable<object[]> GetDraft3Tests()
            => GetAllDraft3Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft3KnownLimitationTests()
            => GetAllDraft3Tests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft3Tests()
            => new TestCaseLoader(new string[] {
                // Draft 3 keyword tests
                // Note: Draft 3 does NOT have: allOf, anyOf, oneOf, not, const, contains, propertyNames, maxProperties, minProperties
                // Draft 3 uses: divisibleBy (not multipleOf), extends (like allOf), disallow (inverse of type)
                // Draft 3 required is a boolean on property definitions, not an array
                "additionalItems",
                "additionalProperties",
                "default",
                "dependencies",
                "disallow",
                "divisibleBy",
                "enum",
                "extends",
                // Note: "format" is excluded - tests annotation-only behavior
                "infinite-loop-detection",
                "items",
                "maximum",
                "maxItems",
                "maxLength",
                "minimum",
                "minItems",
                "minLength",
                "pattern",
                "patternProperties",
                "properties",
                "ref",
                "refRemote",
                "required",
                "type",
                "uniqueItems",

                @"\optional\bignum",
                @"\optional\non-bmp-regex",
                // Note: zeroTerminatedFloats excluded because .NET's System.Text.Json normalizes 1.0 to 1
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft3");

        public static IEnumerable<object[]> GetDraft3FormatAssertionTests()
            => GetAllDraft3FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) == null);

        public static IEnumerable<object[]> GetDraft3FormatAssertionKnownLimitationTests()
            => GetAllDraft3FormatAssertionTests().Where(arr => GetSkipReason(((TestCase)arr[0]).Description) != null);

        private static IEnumerable<object[]> GetAllDraft3FormatAssertionTests()
            => new TestCaseLoader(new string[] {
                @"\optional\ecmascript-regex",
                @"\optional\format\color",
                @"\optional\format\date-time",
                @"\optional\format\date",
                @"\optional\format\email",
                @"\optional\format\host-name",
                @"\optional\format\ip-address",
                @"\optional\format\ipv6",
                @"\optional\format\regex",
                @"\optional\format\time",
                @"\optional\format\uri",
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft3");

        /// <summary>
        /// Returns a skip reason if the test case should be skipped, null otherwise.
        /// Uses standardized skip reason constants from <see cref="SkipReasons"/>.
        /// </summary>
        private static string? GetSkipReason(string testCaseDescription)
        {
            // Draft 3 uses different $ref semantics - $ref overrides sibling keywords
            if (testCaseDescription == "ref overrides any sibling keywords" ||
                testCaseDescription.StartsWith("ref overrides any sibling keywords", StringComparison.Ordinal))
            {
                return SkipReasons.RefOverrideSemantics;
            }

            // Draft 3 uses id: "#fragment" for location-independent identifiers (anchors)
            // In older drafts, $ref overrides sibling id, so id doesn't change the base URI
            if (testCaseDescription == "$ref prevents a sibling id from changing the base uri" ||
                testCaseDescription.StartsWith("$ref prevents a sibling id from changing the base uri", StringComparison.Ordinal))
            {
                return SkipReasons.RefOverrideSemantics;
            }

            return null;
        }
    }
}
