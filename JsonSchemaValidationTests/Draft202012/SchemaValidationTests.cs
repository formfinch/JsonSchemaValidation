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

namespace JsonSchemaValidationTests.Draft202012
{
    public class SchemaValidationTests
    {
        [Theory]
        [MemberData(nameof(GetDraft202012Tests))]
        public void Draft202012Tests(TestCase testCase)
        {
            var schemaRepository = new SchemaRepository();
            Uri schemaUri = schemaRepository.AddSchema(testCase.Schema, SchemaRepositoryHelpers.GenerateRandomSchemaId());
            var schemaValidator = new SchemaValidator(schemaRepository, JsonSchemaDraft.Factories);

            foreach (var test in testCase.Tests)
            {
                var validationResult = schemaValidator.Validate(schemaUri, test.GetProperty("data"));
                var expectedResult = test.GetProperty("valid").GetBoolean();
                Assert.Equal(expectedResult, validationResult.IsValid);
            }
        }

        public static IEnumerable<object[]> GetDraft202012Tests()
            => new TestCaseLoader(new string[] {  
                /* implemented keyword tests */
                "maximum validation", "maximum validation with unsigned integer",
                "minimum validation", "minimum validation with signed integer"


            }).LoadTestCases(@"..\..\..\..\submodules\JSON-Schema-Test-Suite\tests\draft2020-12");
    }
}
