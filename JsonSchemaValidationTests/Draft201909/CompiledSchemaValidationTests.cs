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
            var remotesPath = @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes";
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

                // Extract $defs subschemas that are self-contained
                if (root.TryGetProperty("$defs", out var defs) && defs.ValueKind == JsonValueKind.Object)
                {
                    foreach (var def in defs.EnumerateObject())
                    {
                        var subschemaContent = def.Value.GetRawText();
                        if (!subschemaContent.Contains("\"$ref\"") && !subschemaContent.Contains("\"$recursiveRef\""))
                        {
                            var fragmentUri = new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#/$defs/{def.Name}");
                            schemas.Add((fragmentUri, subschemaContent));
                        }
                    }
                }

                // Also check definitions (Draft 7 style)
                if (root.TryGetProperty("definitions", out var definitions) && definitions.ValueKind == JsonValueKind.Object)
                {
                    foreach (var def in definitions.EnumerateObject())
                    {
                        var subschemaContent = def.Value.GetRawText();
                        if (!subschemaContent.Contains("\"$ref\"") && !subschemaContent.Contains("\"$recursiveRef\""))
                        {
                            var fragmentUri = new Uri($"{baseUri.GetLeftPart(UriPartial.Query)}#/definitions/{def.Name}");
                            schemas.Add((fragmentUri, subschemaContent));
                        }
                    }
                }

                ExtractSelfContainedAnchors(schemas, baseUri, root);
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        private static void ExtractSelfContainedAnchors(List<(Uri SchemaUri, string Content)> schemas, Uri baseUri, JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return;

            var content = element.GetRawText();

            if (!content.Contains("\"$ref\"") && !content.Contains("\"$recursiveRef\""))
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

                // Check for $recursiveAnchor
                if (element.TryGetProperty("$recursiveAnchor", out var recursiveAnchor) &&
                    recursiveAnchor.ValueKind == JsonValueKind.True)
                {
                    // $recursiveAnchor doesn't create a named anchor, but we note it for reference
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

            if (element.TryGetProperty("definitions", out var definitions) && definitions.ValueKind == JsonValueKind.Object)
            {
                foreach (var def in definitions.EnumerateObject())
                {
                    ExtractSelfContainedAnchors(schemas, baseUri, def.Value);
                }
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

        [SkippableTheory]
        [MemberData(nameof(GetDraft201909Tests))]
        public void Draft201909CompiledTests(TestCase testCase)
        {
            var skipReason = GetSkipReason(testCase.Description);
            Skip.If(skipReason != null, skipReason);
            RunTestCase(testCase);
        }

        [SkippableTheory]
        [MemberData(nameof(GetDraft201909FormatAssertionTests))]
        public void Draft201909CompiledFormatAssertionTests(TestCase testCase)
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

                @"\optional\anchor",
                @"\optional\bignum",
                @"\optional\cross-draft",
                @"\optional\dependencies-compatibility",
                @"\optional\float-overflow",
                @"\optional\format-assertion",
                @"\optional\id",
                @"\optional\no-schema",
                @"\optional\non-bmp-regex",
                @"\optional\refOfUnknownKeyword",
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2019-09");

        public static IEnumerable<object[]> GetDraft201909FormatAssertionTests()
            => new TestCaseLoader(new string[] {
                @"\optional\ecmascript-regex",
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
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2019-09");

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
                "unevaluatedItems with items and additionalItems",
                "unevaluatedItems with nested items and additionalItems",
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
                "unevaluatedItems with tuple",
                "unevaluatedItems with nested tuple",
            };

            if (itemsArrayTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.ItemsAsArrayNotSupported;
            }

            // $recursiveRef tests requiring runtime recursive scope resolution
            // Skip all $recursiveRef tests as compiled validators can't handle runtime recursive resolution
            if (testCaseDescription.StartsWith("$recursiveRef", StringComparison.Ordinal) ||
                testCaseDescription.StartsWith("dynamic $recursiveRef", StringComparison.Ordinal) ||
                testCaseDescription.StartsWith("multiple dynamic paths to the $recursiveRef", StringComparison.Ordinal) ||
                testCaseDescription.StartsWith("$ref with $recursiveAnchor", StringComparison.Ordinal) ||
                testCaseDescription.Contains("$recursiveRef"))
            {
                return SkipReasons.RecursiveRefResolution;
            }

            // Anchor tests with base URI changes
            var anchorBaseUriTests = new[]
            {
                "Location-independent identifier with base URI change in subschema",
                "same $anchor with different base uri",
            };

            if (anchorBaseUriTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.BaseUriChange;
            }

            // Remote refs with internal references
            var remoteRefTests = new[]
            {
                "ref within remote ref",
                "base URI change - change folder in subschema",
                "root ref in remote ref",
                "Location-independent identifier in remote ref",
                "retrieved nested refs resolve relative to their URI not $id",
                "$ref to $ref finds detached $anchor",
                "relative pointer ref to array",
            };

            if (remoteRefTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.RemoteRefWithInternalRef;
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

            // unevaluatedItems/unevaluatedProperties tests that require annotation tracking
            var unevaluatedTests = new[]
            {
                "unevaluatedItems with anyOf",
                "unevaluatedItems with oneOf",
                "unevaluatedItems with if/then/else",
                "unevaluatedItems with $ref",
                "unevaluatedItems before $ref",
                "item is evaluated in an uncle schema to unevaluatedItems",
                "unevaluatedItems can see annotations from if without",
            };

            if (unevaluatedTests.Any(t => testCaseDescription == t || testCaseDescription.StartsWith(t, StringComparison.Ordinal)))
            {
                return SkipReasons.UnevaluatedNotSupported;
            }

            return null;
        }
    }
}
