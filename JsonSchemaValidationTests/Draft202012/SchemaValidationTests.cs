using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Repositories;
using JsonSchemaValidation.Validation;
using JsonSchemaValidation.Draft202012;
using JsonSchemaValidationTests.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection.Metadata.Ecma335;

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
                 "allOf",
                "anyOf",
                "boolean_schema",
                "const",
                "contains",
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
                // "not", dependent on unevaluatedProperties
                "oneOf",
                "prefixItems",
                "required",
                "type",
                "unevaluatedItems"

                // "uniqueItems" : Disabled, test cases require that items and prefixItems keywords are implemented
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2020-12");

        private bool IsTestDisabled(string testCaseDescription, string testDescription)
        {
            var disabledTests = new Tuple<string, string>[]
            {
                // Test is not valid or disabled because:
                
                // $ref replaces the initial schema containing unevaluatedItems keyword. 
                // unevaluatedItems: false should no longer be used
                new ("unevaluatedItems with $ref", "with unevaluated items"),
            };

            return disabledTests.Any(test => test.Item1 == testCaseDescription && test.Item2 == testDescription);
        }
    }
}
