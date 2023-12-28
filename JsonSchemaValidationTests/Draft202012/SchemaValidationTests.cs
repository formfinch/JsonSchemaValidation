using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Repositories;
using JsonSchemaValidationTests.TestCases;
using Microsoft.Extensions.DependencyInjection;

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
        }

        [Theory]
        [MemberData(nameof(GetDraft202012Tests))]
        public void Draft202012Tests(TestCase testCase)
        {
            var schemaRepository = _serviceProvider.GetRequiredService<ISchemaRepository>();
            var schemaData = schemaRepository.AddSchema(new SchemaMetadata(testCase.Schema));

            var schemaFactory = _serviceProvider.GetRequiredService<ISchemaFactory>();

            var schemaValidatorFactory = _serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
            var schemaValidator = schemaValidatorFactory.GetValidator(schemaData.SchemaUri!);

            var jsonValidationContextFactory = _serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

            foreach (var test in testCase.Tests)
            {
                var testData = test.GetProperty("data");
                var prpDescription = test.GetProperty("description");
                string testDescription = prpDescription.GetString()!;
                if (IsTestDisabled(testCase.Description, testDescription)) continue;

                var context = jsonValidationContextFactory.CreateContextForRoot(testData);
                var validationResult = schemaValidator.Validate(context);
                var expectedResult = test.GetProperty("valid").GetBoolean();
                Assert.Equal(expectedResult, validationResult.IsValid);
            }
        }

        public static IEnumerable<object[]> GetDraft202012Tests()
            => new TestCaseLoader(new string[] {  
                /* implemented keyword tests */
                "additionalProperties",
                "allOf",
                "anyOf",
                "boolean_schema",
                "const",
                "contains",
                "default",
                "enum",
                "exclusiveMaximum",
                "exclusiveMinimum",
                "if-then-else",
                "items",
                "maximum",
                "maxItems",
                "maxLength",
                "minimum",
                "minItems",
                "minLength",
                "multipleOf",
                "not",
                "oneOf",
                "pattern",
                "patternProperties",
                "prefixItems",
                "properties",
                "required",
                "type",
                "unevaluatedItems",

                @"\optional\format\date-time",
                @"\optional\format\date",
                @"\optional\format\email",
                @"\optional\format\idn-email",
                @"\optional\format\time"
                // "uniqueItems" : Disabled, test cases require that items and prefixItems keywords are implemented
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2020-12");

        private bool IsTestDisabled(string testCaseDescription, string testDescription)
        {
            var disabledTests = new Tuple<string, string>[]
            {
                // Test is not valid or disabled because:

                // not keyword test dependent on implementation of unevaluatedProperties
                new ("collect annotations inside a 'not', even if collection is disabled", "unevaluated property"),
                
                // $ref replaces the initial schema containing unevaluatedItems keyword. 
                // unevaluatedItems: false should no longer be used
                new ("unevaluatedItems with $ref", "with unevaluated items"),

                // leap seconds are not supported in date-time format validation
                new ("validation of date-time strings", "a valid date-time with a leap second, UTC"),
                new ("validation of date-time strings", "a valid date-time with a leap second, with minus offset"),

                // leap seconds are not supported in time format validation
                new ("validation of time strings", "a valid time string with leap second, Zulu"),
                new ("validation of time strings", "valid leap second, zero time-offset"),
                new ("validation of time strings", "valid leap second, positive time-offset"),
                new ("validation of time strings", "valid leap second, negative time-offset"),
                new ("validation of time strings", "valid leap second, large positive time-offset"),
                new ("validation of time strings", "valid leap second, large negative time-offset")
            };

            return disabledTests.Any(test => test.Item1 == testCaseDescription && (test.Item2 == "*" || test.Item2 == testDescription));
        }
    }
}
