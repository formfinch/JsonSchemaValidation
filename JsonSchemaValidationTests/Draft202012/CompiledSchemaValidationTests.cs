using System.Text.Json;
using System.Text.Json.Nodes;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Compiler;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using FormFinch.JsonSchemaValidationTests.TestCases;

namespace FormFinch.JsonSchemaValidationTests.Draft202012
{
    /// <summary>
    /// Shared fixture for compiled schema validation tests.
    /// This fixture is created once and shared across all tests, avoiding
    /// expensive re-initialization of the registry and remote schema loading.
    /// </summary>
    public class CompiledSchemaValidationFixture : IDisposable
    {
        public CompiledValidatorRegistry Registry { get; }
        public RuntimeValidatorFactory Factory { get; }

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
            using var factory = new RuntimeValidatorFactory(registry);
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
            // Note: Compiled validators don't have a separate format assertion mode yet.
            // Format validation is always enabled if supported by the compiled validator.
            RunTestCase(testCase);
        }

        private void RunTestCase(TestCase testCase)
        {
            if (IsTestDisabled(testCase.Description, "*")) return;

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
                if (IsTestDisabled(testCase.Description, testDescription)) continue;

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

        private bool IsTestDisabled(string testCaseDescription, string testDescription)
        {
            // These tests are disabled because they require features that compiled validators
            // cannot support due to fundamental architectural limitations:
            //
            // 1. $dynamicRef with dynamic scope resolution - requires runtime stack inspection
            //    to find the first matching $dynamicAnchor in the dynamic call chain.
            //    Compiled validators resolve references statically at compile time.
            //
            // 2. Remote refs with internal references - subschemas extracted from remote files
            //    that contain $ref to sibling definitions can't be compiled standalone because
            //    the internal references can't be resolved without the full document context.
            //
            // 3. Vocabulary-based validation - requires checking $vocabulary in metaschema
            //    to enable/disable keyword processing. Compiled validators don't support this.
            //
            // 4. Cross-draft compatibility - requires processing $ref targets according to
            //    their declared $schema, which requires runtime schema detection.

            var disabledTests = new Tuple<string, string>[]
            {
                // === $dynamicRef tests - require dynamic scope resolution ===
                // The following $dynamicRef tests now pass because they use static resolution patterns:
                // - "A $dynamicRef without anchor in fragment behaves identical to $ref" (JSON pointer fragments)
                // - "An $anchor with the same name as a $dynamicAnchor is not used for dynamic scope resolution"
                // - "A $dynamicRef without a matching $dynamicAnchor in the same schema resource behaves like a normal $ref to $anchor"
                // - "A $dynamicRef with a non-matching $dynamicAnchor in the same schema resource behaves like a normal $ref to $anchor"
                // - "A $dynamicRef resolves to the first $dynamicAnchor still in scope..." (root-level outer anchor)
                // - "A $dynamicRef with intermediate scopes..." (root-level outer anchor)
                // - "$dynamicRef skips over intermediate resources - direct reference" (local resolution only)
                // - "unevaluatedItems with $dynamicRef" (root-level outer anchor with annotation tracking)
                // - "unevaluatedProperties with $dynamicRef" (root-level outer anchor with annotation tracking)
                //
                // These tests require runtime dynamic scope resolution (non-local $dynamicRef or complex paths):
                new("A $dynamicRef that initially resolves to a schema with a matching $dynamicAnchor resolves to the first $dynamicAnchor in the dynamic scope", "*"),
                new("multiple dynamic paths to the $dynamicRef keyword", "*"),
                new("after leaving a dynamic scope, it is not used by a $dynamicRef", "*"),
                new("strict-tree schema, guards against misspelled properties", "*"),
                new("tests for implementation dynamic anchor and reference link", "*"),
                new("$ref and $dynamicAnchor are independent of order - $defs first", "*"),
                new("$ref and $dynamicAnchor are independent of order - $ref first", "*"),
                new("$ref to $dynamicRef finds detached $dynamicAnchor", "*"),
                new("$dynamicRef avoids the root of each schema, but scopes are still registered", "*"),
                new("$dynamicRef skips over intermediate resources - pointer reference across resource boundary", "*"),

                // === anchor tests with base URI changes (2 tests) ===
                new("Location-independent identifier with base URI change in subschema", "*"),
                new("same $anchor with different base uri", "*"),

                // === Remote refs with internal references (5 tests) ===
                // These require the full remote document context to resolve internal $refs
                // Note: "remote ref, containing refs itself" now passes with proper metaschema initialization
                new("ref within remote ref", "*"),
                new("base URI change - change folder in subschema", "*"),
                new("root ref in remote ref", "*"),
                new("Location-independent identifier in remote ref", "*"),
                new("retrieved nested refs resolve relative to their URI not $id", "*"),
                new("$ref to $ref finds detached $anchor", "*"),

                // === Vocabulary tests (1 test) ===
                // "validate definition against metaschema" now passes with dynamic scope root support.
                // $dynamicRef in meta/core resolves to the outermost scope root (Draft202012Schema)
                // which validates against all vocabulary schemas including meta/validation.
                new("schema that uses custom metaschema with with no validation vocabulary", "*"),

                // === Cross-draft tests (1 test) ===
                new("refs to historic drafts are processed as historic drafts", "*"),
            };

            return disabledTests.Any(test =>
                (test.Item1 == testCaseDescription || testCaseDescription.StartsWith(test.Item1)) &&
                (test.Item2 == "*" || test.Item2 == testDescription));
        }
    }
}
