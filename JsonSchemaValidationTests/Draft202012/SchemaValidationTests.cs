using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidationTests.TestCases;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace JsonSchemaValidationTests.Draft202012
{
    public class SchemaValidationTests
    {
        private readonly IServiceProvider _serviceProvider;

        public SchemaValidationTests()
        {
            // Initialize DI container
            var services = new ServiceCollection();

            // Add your services
            services.AddJsonSchemaValidation(opt =>
            {
                opt.EnableDraft202012 = true;
            });

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
            _serviceProvider.InitializeSingletonServices();

            // Load remote schemas required for vocabulary tests
            LoadRemoteSchemas();
        }

        private void LoadRemoteSchemas()
        {
            var schemaRepository = _serviceProvider.GetRequiredService<ISchemaRepository>();

            // Load draft2020-12 specific remote schemas
            LoadRemoteSchemasFromPath(
                schemaRepository,
                @"..\..\..\..\submodules\JSON-Schema-Test-Suite\remotes\draft2020-12",
                "http://localhost:1234/draft2020-12/");

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
        [MemberData(nameof(GetDraft202012Tests))]
        public void Draft202012Tests(TestCase testCase)
        {
            if (IsTestDisabled(testCase.Description, "*")) return;

            var schemaRepository = _serviceProvider.GetRequiredService<ISchemaRepository>();
            var schemaValidatorFactory = _serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
            var jsonValidationContextFactory = _serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

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
                var validationResult = schemaValidator.Validate(context);

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
                // "format",
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

                @"\optional\bignum",
                // @"\optional\cross-draft",                    // No cross-draft compatibility yet
                @"\optional\dependencies-compatibility",
                // @"\optional\ecmascript-regex",               // Regexes are not implemented with compatibility for Ecmascript in mind.
                // @"\optional\float-overflow",                 // Dont know how to handle the test case 1e308
                // @"\optional\format-assertion",               // Requires vocabulary support
                @"\optional\no-schema",
                // @"\optional\non-bmp-regex",                  // Fails but low priority to fix
                @"\optional\refOfUnknownKeyword"
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2020-12");

        private bool IsTestDisabled(string testCaseDescription, string testDescription)
        {
            var disabledTests = new Tuple<string, string>[]
            {
                // Test is not valid or disabled because:

                // leap seconds are not supported in date-time format validation
                new ("validation of date-time strings", "a valid date-time with a leap second, UTC"),
                new ("validation of date-time strings", "a valid date-time with a leap second, with minus offset"),

                // leap seconds are not supported in time format validation
                new ("validation of time strings", "a valid time string with leap second, Zulu"),
                new ("validation of time strings", "valid leap second, zero time-offset"),
                new ("validation of time strings", "valid leap second, positive time-offset"),
                new ("validation of time strings", "valid leap second, negative time-offset"),
                new ("validation of time strings", "valid leap second, large positive time-offset"),
                new ("validation of time strings", "valid leap second, large negative time-offset"),

                // check for invalid unicode characters or contextual rules in idn-hostname is beyond scope
                new ("validation of internationalized host names", "contains illegal char U+302E Hangul single dot tone mark"),
                new ("validation of internationalized host names", "Exceptions that are DISALLOWED, right-to-left chars"),
                new ("validation of internationalized host names", "Exceptions that are DISALLOWED, left-to-right chars"),
                new ("validation of internationalized host names", "MIDDLE DOT with no preceding 'l'"),
                new ("validation of internationalized host names", "MIDDLE DOT with nothing preceding"),
                new ("validation of internationalized host names", "MIDDLE DOT with no following 'l'"),
                new ("validation of internationalized host names", "MIDDLE DOT with nothing following"),
                new ("validation of internationalized host names", "Greek KERAIA not followed by Greek"),
                new ("validation of internationalized host names", "Greek KERAIA not followed by anything"),
                new ("validation of internationalized host names", "Hebrew GERESH not preceded by Hebrew"),
                new ("validation of internationalized host names", "Hebrew GERESH not preceded by anything"),
                new ("validation of internationalized host names", "Hebrew GERSHAYIM not preceded by Hebrew"),
                new ("validation of internationalized host names", "Hebrew GERSHAYIM not preceded by anything"),
                new ("validation of internationalized host names", "KATAKANA MIDDLE DOT with no Hiragana, Katakana, or Han"),
                new ("validation of internationalized host names", "KATAKANA MIDDLE DOT with no other characters"),
                new ("validation of internationalized host names", "Arabic-Indic digits mixed with Extended Arabic-Indic digits"),

                // conflicting tests in hostname and idn-hostname, for now no check on this
                new ("validation of internationalized host names", "U-label contains \"--\" in the 3rd and 4th position")
            };

            return disabledTests.Any(test => test.Item1 == testCaseDescription && (test.Item2 == "*" || test.Item2 == testDescription));
        }
    }
}
