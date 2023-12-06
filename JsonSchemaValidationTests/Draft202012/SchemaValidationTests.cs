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

            var schemaValidatorFactory = _serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
            var schemaValidator = schemaValidatorFactory.GetValidator(schemaData.SchemaUri!);

            foreach (var test in testCase.Tests)
            {
                var validationResult = schemaValidator.Validate(test.GetProperty("data"));
                var expectedResult = test.GetProperty("valid").GetBoolean();
                Assert.Equal(expectedResult, validationResult.IsValid);
            }
        }

        public static IEnumerable<object[]> GetDraft202012Tests()
            => new TestCaseLoader(new string[] {  
                /* implemented keyword tests */
                "const",
                "exclusiveMaximum",
                "exclusiveMinimum",
                "items",
                "maximum",
                "maxItems",
                "maxLength",
                "minimum",
                "minItems",
                "minLength",
                "multipleOf",

                // todo: tests contain boolean item schemas which are not yet supported
                // "prefixItems",

                "required",
                "type"

                // "uniqueItems" : Disabled, test cases require that items and prefixItems keywords are implemented
            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2020-12");
    }
}
