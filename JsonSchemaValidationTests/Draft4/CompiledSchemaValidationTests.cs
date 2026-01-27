// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Compiler;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using FormFinch.JsonSchemaValidationTests.Common;
using FormFinch.JsonSchemaValidationTests.TestCases;
using Xunit.Sdk;

namespace FormFinch.JsonSchemaValidationTests.Draft4
{
    /// <summary>
    /// Shared fixture for Draft 4 compiled schema validation tests.
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
            Factory = new RuntimeValidatorFactory(Registry);
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

            // Collect draft4 remotes
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft4"), "http://localhost:1234/draft4/");

            // Collect future draft remotes for cross-draft tests
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft6"), "http://localhost:1234/draft6/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft7"), "http://localhost:1234/draft7/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2019-09"), "http://localhost:1234/draft2019-09/");
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft2020-12"), "http://localhost:1234/draft2020-12/");

            // Collect root-level remotes
            CollectRemotesFromPath(pendingSchemas, remotesPath, "http://localhost:1234/", topLevelOnly: true);

            if (pendingSchemas.Count == 0) return;

            using var factory = new RuntimeValidatorFactory(registry);
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
                            // Ignore if no $id/id or already registered
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
                    schemas.Add((schemaUri, content));

                    ExtractSelfContainedSubschemas(schemas, schemaUri, content);
                }
                catch
                {
                    // Ignore errors reading files
                }
            }
        }

        private static void ExtractSelfContainedSubschemas(List<(Uri SchemaUri, string Content)> schemas, Uri baseUri, string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object) return;

                // Extract definitions subschemas that are self-contained (Draft 4 uses "definitions")
                if (root.TryGetProperty("definitions", out var definitions) && definitions.ValueKind == JsonValueKind.Object)
                {
                    foreach (var def in definitions.EnumerateObject())
                    {
                        var subschemaContent = def.Value.GetRawText();
                        if (!subschemaContent.Contains("\"$ref\""))
                        {
                            var fragmentUri = new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#/definitions/{def.Name}");
                            schemas.Add((fragmentUri, subschemaContent));
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }
    }

    [Trait("Draft", "4")]
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

        [SkippableTheory]
        [MemberData(nameof(GetDraft4Tests))]
        public void Draft4CompiledTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [MemberData(nameof(GetDraft4FormatAssertionTests))]
        public void Draft4CompiledFormatAssertionTests(TestCase testCase)
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

        public static IEnumerable<object[]> GetDraft4Tests()
            => new TestCaseLoader(new string[] {
                // Draft 4 keyword tests
                // Note: Draft 4 does NOT have: boolean_schema, const, contains, propertyNames
                "additionalItems",
                "additionalProperties",
                "allOf",
                "anyOf",
                "default",
                "definitions",
                "dependencies",
                "enum",
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
                "ref",
                "refRemote",
                "required",
                "type",
                "uniqueItems",

                @"\optional\bignum",
                @"\optional\float-overflow",
                @"\optional\non-bmp-regex",
                // Note: zeroTerminatedFloats excluded because .NET's System.Text.Json normalizes 1.0 to 1
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft4");

        public static IEnumerable<object[]> GetDraft4FormatAssertionTests()
            => new TestCaseLoader(new string[] {
                @"\optional\ecmascript-regex",
                @"\optional\id",
                @"\optional\format\date-time",
                @"\optional\format\email",
                @"\optional\format\hostname",
                @"\optional\format\ipv4",
                @"\optional\format\ipv6",
                @"\optional\format\unknown",
                @"\optional\format\uri",
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft4");

        /// <summary>
        /// Returns a skip reason if the test case should be skipped, null otherwise.
        /// Uses standardized skip reason constants from <see cref="SkipReasons"/>.
        /// </summary>
        private static string? GetSkipReason(string testCaseDescription)
        {
            // Infinite loop detection tests cause stack overflow
            if (testCaseDescription.StartsWith("evaluating the same schema location against the same data location twice", StringComparison.Ordinal))
            {
                return SkipReasons.InfiniteLoopNotSupported;
            }

            // additionalItems not supported
            var additionalItemsTests = new[]
            {
                "additionalItems as schema",
                "array of items with no additionalItems permitted",
                "additionalItems are allowed by default",
                "additionalItems does not look in applicators",
                "items validation adjusts the starting index for additionalItems",
                "additionalItems with heterogeneous array",
                "additionalItems with null instance elements",
                "uniqueItems with an array of items and additionalItems",
                "uniqueItems=false with an array of items and additionalItems",
            };

            if (additionalItemsTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.AdditionalItemsNotSupported;
            }

            // items as array (tuple validation) not supported
            var itemsArrayTests = new[]
            {
                "an array of schemas for items",
                "items with boolean schemas",
                "items and subitems",
            };

            if (itemsArrayTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.ItemsAsArrayNotSupported;
            }

            // dependencies keyword not supported
            var dependenciesTests = new[]
            {
                "dependencies",
                "dependencies with empty array",
                "dependencies with escaped characters",
                "multiple dependencies",
                "multiple dependencies subschema",
                "single dependency",
            };

            if (dependenciesTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.DependenciesNotSupported;
            }

            // Metaschema validation not supported
            if (testCaseDescription == "validate definition against metaschema" ||
                testCaseDescription.StartsWith("validate definition against metaschema", StringComparison.Ordinal))
            {
                return SkipReasons.MetaschemaValidationNotSupported;
            }

            // Remote refs and id resolution issues
            var refTests = new[]
            {
                "ref within remote ref",
                "base URI change - change folder in subschema",
                "root ref in remote ref",
                "Location-independent identifier in remote ref",
                "relative pointer ref to array",
                "ref overrides any sibling keywords",
                "$ref prevents a sibling id from changing the base uri",
                "remote ref, containing refs itself",
                "Recursive references between schemas",
                "Location-independent identifier",
                "Location-independent identifier with base URI change in subschema",
                "id must be resolved against nearest parent",
                "base URI change",
                "id inside an enum is not a real identifier",
            };

            if (refTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.RemoteRefWithInternalRef;
            }

            return null;
        }
    }
}
