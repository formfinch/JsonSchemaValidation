using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidationTests.TestCases;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidationTests.Draft7
{
    [Trait("Draft", "7")]
    public class SchemaValidationTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceProvider _serviceProviderWithFormatAssertion;
        private readonly IServiceProvider _serviceProviderWithContentAssertion;

        public SchemaValidationTests()
        {
            // Initialize DI container with default settings (format annotation-only)
            // Set Draft 7 as default for test schemas without $schema
            var services = new ServiceCollection();
            services.AddJsonSchemaValidation(opt =>
            {
                opt.EnableDraft7 = true;
                opt.DefaultDraftVersion = "http://json-schema.org/draft-07/schema";
            });
            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.InitializeSingletonServices();
            LoadRemoteSchemas(_serviceProvider);

            // Initialize DI container with format assertion enabled
            var servicesWithFormatAssertion = new ServiceCollection();
            servicesWithFormatAssertion.AddJsonSchemaValidation(opt =>
            {
                opt.EnableDraft7 = true;
                opt.DefaultDraftVersion = "http://json-schema.org/draft-07/schema";
                opt.Draft7.FormatAssertionEnabled = true;
            });
            _serviceProviderWithFormatAssertion = servicesWithFormatAssertion.BuildServiceProvider();
            _serviceProviderWithFormatAssertion.InitializeSingletonServices();
            LoadRemoteSchemas(_serviceProviderWithFormatAssertion);

            // Initialize DI container with content assertion enabled
            var servicesWithContentAssertion = new ServiceCollection();
            servicesWithContentAssertion.AddJsonSchemaValidation(opt =>
            {
                opt.EnableDraft7 = true;
                opt.DefaultDraftVersion = "http://json-schema.org/draft-07/schema";
                opt.Draft7.ContentAssertionEnabled = true;
            });
            _serviceProviderWithContentAssertion = servicesWithContentAssertion.BuildServiceProvider();
            _serviceProviderWithContentAssertion.InitializeSingletonServices();
            LoadRemoteSchemas(_serviceProviderWithContentAssertion);
        }

        private void LoadRemoteSchemas(IServiceProvider serviceProvider)
        {
            var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

            // Load draft7 specific remote schemas
            LoadRemoteSchemasFromPath(
                schemaRepository,
                @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes\draft7",
                "http://localhost:1234/draft7/");

            // Load future draft remotes for cross-draft tests
            LoadRemoteSchemasFromPath(
                schemaRepository,
                @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes\draft2019-09",
                "http://localhost:1234/draft2019-09/");

            LoadRemoteSchemasFromPath(
                schemaRepository,
                @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes\draft2020-12",
                "http://localhost:1234/draft2020-12/");

            // Load common remote schemas from root remotes folder (including subdirectories)
            LoadRemoteSchemasFromPath(
                schemaRepository,
                @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes",
                "http://localhost:1234/",
                excludeDirectories: new[] { "draft7", "draft2019-09", "draft2020-12", "draft3", "draft4", "draft6", "v1" });
        }

        private void LoadRemoteSchemasFromPath(ISchemaRepository schemaRepository, string remotesPath, string baseUrl, bool topLevelOnly = false, string[]? excludeDirectories = null)
        {
            if (!Directory.Exists(remotesPath)) return;

            var searchOption = topLevelOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

            foreach (var file in Directory.GetFiles(remotesPath, "*.json", searchOption))
            {
                try
                {
                    // Calculate the relative path to build the proper URI
                    var relativePath = Path.GetRelativePath(remotesPath, file).Replace("\\", "/");

                    // Skip files in excluded directories
                    if (excludeDirectories != null)
                    {
                        var firstDir = relativePath.Split('/')[0];
                        if (excludeDirectories.Contains(firstDir))
                            continue;
                    }

                    var content = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(content);

                    var schemaUri = new Uri(baseUrl + relativePath);

                    schemaRepository.TryRegisterSchema(doc.RootElement.Clone(), schemaUri, out _);
                }
                catch
                {
                    // Ignore errors loading remote schemas
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetDraft7Tests))]
        public void Draft7Tests(TestCase testCase)
        {
            RunTestCase(testCase, _serviceProvider);
        }

        [Theory]
        [MemberData(nameof(GetDraft7FormatAssertionTests))]
        public void Draft7FormatAssertionTests(TestCase testCase)
        {
            RunTestCase(testCase, _serviceProviderWithFormatAssertion);
        }

        [Theory]
        [MemberData(nameof(GetDraft7ContentAssertionTests))]
        public void Draft7ContentAssertionTests(TestCase testCase)
        {
            RunTestCase(testCase, _serviceProviderWithContentAssertion);
        }

        private void RunTestCase(TestCase testCase, IServiceProvider serviceProvider)
        {
            if (IsTestDisabled(testCase.Description, "*")) return;

            var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
            var schemaValidatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
            var jsonValidationContextFactory = serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

            if (!schemaRepository.TryRegisterSchema(testCase.Schema, out var schemaData))
            {
                throw new InvalidOperationException(@$"Schema could not be registered.");
            }
            var schemaValidator = schemaValidatorFactory.GetValidator(schemaData!.SchemaUri!);

            foreach (var test in testCase.Tests)
            {
                var prpDescription = test.GetProperty("description");
                string testDescription = prpDescription.GetString()!;
                if (IsTestDisabled(testCase.Description, testDescription)) continue;

                var testData = test.GetProperty("data");
                var expectedResult = test.GetProperty("valid").GetBoolean();
                var context = jsonValidationContextFactory.CreateContextForRoot(testData);
                var validationResult = schemaValidator.ValidateRoot(context);

                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    Assert.Equal(expectedResult, validationResult.IsValid);
                }
                else
                {
                    try
                    {
                        Assert.Equal(expectedResult, validationResult.IsValid);
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine($"\"{testDescription}\"");
                    }
                }
            }
        }

        public static IEnumerable<object[]> GetDraft7Tests()
            => new TestCaseLoader(new string[] {
                // Draft 7 keyword tests
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
                "format",  // Tests annotation-only behavior (no validation)
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

                @"\optional\bignum",
                // Note: content tests removed - they require ContentAssertionEnabled
                @"\optional\cross-draft",
                @"\optional\float-overflow",
                @"\optional\non-bmp-regex",
                @"\optional\unknownKeyword",
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft7");

        /// <summary>
        /// Returns test cases for optional format validation tests.
        /// These tests run with FormatAssertionEnabled = true.
        /// </summary>
        public static IEnumerable<object[]> GetDraft7FormatAssertionTests()
            => new TestCaseLoader(new string[] {
                @"\optional\ecmascript-regex",
                @"\optional\format\date-time",
                @"\optional\format\date",
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
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft7");

        /// <summary>
        /// Returns test cases for optional content validation tests.
        /// These tests run with ContentAssertionEnabled = true.
        /// </summary>
        public static IEnumerable<object[]> GetDraft7ContentAssertionTests()
            => new TestCaseLoader(new string[] {
                @"\optional\content",
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft7");

        private bool IsTestDisabled(string testCaseDescription, string testDescription)
        {
            var disabledTests = new Tuple<string, string>[]
            {
                // No tests are currently disabled
            };

            return disabledTests.Any(test => test.Item1 == testCaseDescription && (test.Item2 == "*" || test.Item2 == testDescription));
        }
    }
}
