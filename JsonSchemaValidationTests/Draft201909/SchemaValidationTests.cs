using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidationTests.TestCases;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidationTests.Draft201909
{
    [Trait("Draft", "2019-09")]
    public class SchemaValidationTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceProvider _serviceProviderWithFormatAssertion;

        public SchemaValidationTests()
        {
            // Initialize DI container with default settings (format annotation-only)
            var services = new ServiceCollection();
            services.AddJsonSchemaValidation(opt =>
            {
                opt.EnableDraft201909 = true;
            });
            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.InitializeSingletonServices();
            LoadRemoteSchemas(_serviceProvider);

            // Initialize DI container with format assertion enabled
            var servicesWithFormatAssertion = new ServiceCollection();
            servicesWithFormatAssertion.AddJsonSchemaValidation(opt =>
            {
                opt.EnableDraft201909 = true;
                opt.FormatAssertionEnabled = true;
            });
            _serviceProviderWithFormatAssertion = servicesWithFormatAssertion.BuildServiceProvider();
            _serviceProviderWithFormatAssertion.InitializeSingletonServices();
            LoadRemoteSchemas(_serviceProviderWithFormatAssertion);
        }

        private void LoadRemoteSchemas(IServiceProvider serviceProvider)
        {
            var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

            // Load draft2019-09 specific remote schemas
            LoadRemoteSchemasFromPath(
                schemaRepository,
                @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes\draft2019-09",
                "http://localhost:1234/draft2019-09/");

            // Load common remote schemas from root remotes folder
            LoadRemoteSchemasFromPath(
                schemaRepository,
                @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes",
                "http://localhost:1234/",
                topLevelOnly: true);
        }

        private void LoadRemoteSchemasFromPath(ISchemaRepository schemaRepository, string remotesPath, string baseUrl, bool topLevelOnly = false)
        {
            if (!Directory.Exists(remotesPath)) return;

            var searchOption = topLevelOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

            foreach (var file in Directory.GetFiles(remotesPath, "*.json", searchOption))
            {
                try
                {
                    var content = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(content);

                    // Calculate the relative path to build the proper URI
                    var relativePath = Path.GetRelativePath(remotesPath, file).Replace("\\", "/");
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
        [MemberData(nameof(GetDraft201909Tests))]
        public void Draft201909Tests(TestCase testCase)
        {
            RunTestCase(testCase, _serviceProvider);
        }

        [Theory]
        [MemberData(nameof(GetDraft201909FormatAssertionTests))]
        public void Draft201909FormatAssertionTests(TestCase testCase)
        {
            RunTestCase(testCase, _serviceProviderWithFormatAssertion);
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

        public static IEnumerable<object[]> GetDraft201909Tests()
            => new TestCaseLoader(new string[] {
                // Implemented keyword tests
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
                "format",  // Tests annotation-only behavior (no validation)
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

                @"\optional\bignum",
                // @"\optional\cross-draft",                    // No cross-draft compatibility yet
                @"\optional\dependencies-compatibility",
                @"\optional\float-overflow",
                @"\optional\format-assertion",                  // Requires vocabulary support
                @"\optional\no-schema",
                @"\optional\non-bmp-regex",
                @"\optional\refOfUnknownKeyword"
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2019-09");

        /// <summary>
        /// Returns test cases for optional format validation tests.
        /// These tests run with FormatAssertionEnabled = true.
        /// </summary>
        public static IEnumerable<object[]> GetDraft201909FormatAssertionTests()
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
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2019-09");

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
