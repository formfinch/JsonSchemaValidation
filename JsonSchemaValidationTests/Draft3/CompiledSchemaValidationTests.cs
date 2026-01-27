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

            // Collect draft3 remotes
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft3"), "http://localhost:1234/draft3/");

            // Collect future draft remotes for cross-draft tests
            CollectRemotesFromPath(pendingSchemas, Path.Combine(remotesPath, "draft4"), "http://localhost:1234/draft4/");
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
                }
                catch
                {
                    // Ignore errors reading files
                }
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

        [SkippableTheory]
        [MemberData(nameof(GetDraft3Tests))]
        public void Draft3CompiledTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [MemberData(nameof(GetDraft3FormatAssertionTests))]
        public void Draft3CompiledFormatAssertionTests(TestCase testCase)
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
                "additionalItems with heterogeneous array",
                "additionalItems are allowed by default",
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
            };

            if (itemsArrayTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.ItemsAsArrayNotSupported;
            }

            // Draft 3 specific keywords: divisibleBy, disallow, extends
            var draft3KeywordTests = new[]
            {
                "by int",
                "by number",
                "by small number",
                "disallow",
                "multiple disallow",
                "extends",
                "multiple extends",
                "extends simple types",
            };

            if (draft3KeywordTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.Draft3KeywordsNotSupported;
            }

            // Draft 3 type with schemas not supported
            var typeSchemaTests = new[]
            {
                "types can include schemas",
                "types from separate schemas are merged",
            };

            if (typeSchemaTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.Draft3TypeSchemasNotSupported;
            }

            // Draft 3 format names not supported
            var formatTests = new[]
            {
                "validation of CSS colors",
                "validation of host names",
                "validation of IP addresses",
                "validation of time strings",
            };

            if (formatTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.Draft3FormatNamesNotSupported;
            }

            // dependencies keyword not supported
            if (testCaseDescription == "dependencies" || testCaseDescription.StartsWith("dependencies", StringComparison.Ordinal))
            {
                return SkipReasons.DependenciesNotSupported;
            }

            // Draft 3 required format (boolean on properties) not supported
            var requiredTests = new[]
            {
                "required validation",
                "required default validation",
                "required with empty array",
            };

            if (requiredTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.Draft3RequiredNotSupported;
            }

            // ref-related issues
            var refTests = new[]
            {
                "ref within remote ref",
                "base URI change - change folder in subschema",
                "root ref in remote ref",
                "relative pointer ref to array",
                "ref overrides any sibling keywords",
                "$ref prevents a sibling id from changing the base uri",
                "remote ref, containing refs itself",
                "fragment within remote ref",
                "change resolution scope",
                "enums in properties",
                "applies a nested schema",
            };

            if (refTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.RemoteRefWithInternalRef;
            }

            return null;
        }
    }
}
