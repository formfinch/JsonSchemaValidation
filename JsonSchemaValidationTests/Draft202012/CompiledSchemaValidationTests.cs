using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Compiler;
using JsonSchemaValidation.CompiledValidators;
using JsonSchemaValidationTests.TestCases;

namespace JsonSchemaValidationTests.Draft202012
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

            // Pre-register all metaschemas so they can be resolved by external $ref
            foreach (var metaschema in CompiledMetaschemas.GetAll())
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

            // Load remote schemas for test suite compatibility
            LoadRemoteSchemas(registry);

            return registry;
        }

        private static void LoadRemoteSchemas(CompiledValidatorRegistry registry)
        {
            var remotesPath = @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes";
            if (!Directory.Exists(remotesPath)) return;

            // Collect all remote schema files first
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
                }
                catch
                {
                    // Ignore errors reading files
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
            var disabledTests = new Tuple<string, string>[]
            {
                // $dynamicRef - compiled validators don't support dynamic reference resolution
                new("A $dynamicRef without anchor in fragment behaves identical to $ref", "*"),
                new("An $anchor with the same name as a $dynamicAnchor is not used for dynamic scope resolution", "*"),
                new("A $dynamicRef without a matching $dynamicAnchor in the same schema resource behaves like a normal $ref to $anchor", "*"),
                new("A $dynamicRef with a non-matching $dynamicAnchor in the same schema resource behaves like a normal $ref to $anchor", "*"),
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

                // anchor - $id with base URI change
                new("Location-independent identifier with base URI change in subschema", "*"),
                new("same $anchor with different base uri", "*"),

                // Remote refs - compiled validators have issues with external refs containing fragments
                new("remote ref, containing refs itself", "*"),
                new("fragment within remote ref", "*"),
                new("anchor within remote ref", "*"),
                new("ref within remote ref", "*"),
                new("base URI change - change folder in subschema", "*"),
                new("root ref in remote ref", "*"),
                new("Location-independent identifier in remote ref", "*"),
                new("retrieved nested refs resolve relative to their URI not $id", "*"),
                new("$ref to $ref finds detached $anchor", "*"),

                // Vocabulary - validate definition against metaschema
                new("validate definition against metaschema", "*"),

                // Unevaluated edge cases
                new("Evaluated items collection needs to consider instance location", "*"),
                new("nested unevaluatedProperties, outer true, inner false, properties outside", "*"),
                new("in-place applicator siblings, anyOf has unevaluated", "*"),
                new("Evaluated properties collection needs to consider instance location", "*"),

                // Format-assertion vocabulary tests
                new("schema that uses custom metaschema with with no validation vocabulary", "*"),
                new("schema that uses custom metaschema with format-assertion: false", "*"),
                new("schema that uses custom metaschema with format-assertion: true", "*"),

                // Cross-draft tests
                new("refs to historic drafts are processed as historic drafts", "*"),
            };

            return disabledTests.Any(test =>
                (test.Item1 == testCaseDescription || testCaseDescription.StartsWith(test.Item1)) &&
                (test.Item2 == "*" || test.Item2 == testDescription));
        }
    }
}
