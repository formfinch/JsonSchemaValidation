using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Repositories;
using JsonSchemaValidationTests.TestCases;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using static System.Formats.Asn1.AsnWriter;

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
                /* implemented keyword tests */
                "additionalProperties",
                "allOf",
                "anchor",
                "anyOf",
                "boolean_schema",
                "const",
                "contains",
                // "content",
                "default",
                "defs",
                // "dependentRequired",
                // "dependentSchemas",
                "dynamicRef",
                "enum",
                "exclusiveMaximum",
                "exclusiveMinimum",
                // "format",
                "id",
                "if-then-else",
                "infinite-loop-detection",
                "items",
                // "maxContains",
                "maximum",
                "maxItems",
                "maxLength",
                // "maxProperties",
                // "minContains",
                "minimum",
                "minItems",
                "minLength",
                // "minProperties",
                "multipleOf",
                "not",
                "oneOf",
                "pattern",
                "patternProperties",
                "prefixItems",
                "properties",
                // "propertyNames",
                "ref",
                // "refRemote",
                "required",
                "type",
                "unevaluatedItems",
                // "unevaluatedProperties",
                "uniqueItems",
                "unknownKeyword",
                // "vocabulary"

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
                @"\optional\format\uuid"
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
                new ("validation of internationalized host names", "U-label contains \"--\" in the 3rd and 4th position"),

                // test.data is a schema that is validated against the meta schema
                // the meta schema does not define use of $id in the $defs section
                new ("Invalid use of fragments in location-independent $id", "*"),

                // root pointer ref is not supported
                // root pointer ref creates cyclic-reference because of the method of implementation
                new ("root pointer ref", "*" ),

                // test logic is correct but currently unsupported because of misunderstanding $ref keyword.
                // The package misinterprets $ref as replacing rather than coexisting with sibling keywords
                // ignoring its intended applicator function.
                
                // test succeeds with change to allOf: [ { $ref... } ]
                new ("ref applies alongside sibling keywords", "*" ),
                
                // unable to solve cyclic references in any way
                new ("Recursive references between schemas", "*" ),

                new ("ref creates new scope when adjacent to keywords", "*"),
                
                // test has out of reach reference
                // we dont yet go through the complete schema to get $ids from items that otherwise never get handled.
                new ("refs with relative uris and defs", "*"),
                new ("relative refs with absolute uris and defs", "*"),

                // urn type not supported by .NET?
                // strange these tests were not presented in the format section
                new ("simple URN base URI with $ref via the URN", "*"),
                new ("simple URN base URI with JSON pointer", "*"),
                new ("URN base URI with NSS", "*"),
                new ("URN base URI with r-component", "*"),
                new ("URN base URI with q-component", "*"),
                new ("URN base URI with URN and JSON pointer ref", "*"),
                new ("URN base URI with URN and anchor ref", "*"),
                new ("URN ref with nested pointer ref", "*"),

                // $dynamicRef and $dynamicAnchor needs work                
                new ("A $dynamicRef that initially resolves to a schema with a matching $dynamicAnchor resolves to the first $dynamicAnchor in the dynamic scope", "*"),
                new ("multiple dynamic paths to the $dynamicRef keyword", "*"),
                new ("after leaving a dynamic scope, it is not used by a $dynamicRef", "*"),
                new ("strict-tree schema, guards against misspelled properties", "*"),
                new ("tests for implementation dynamic anchor and reference link", "*"),
                new ("$ref and $dynamicAnchor are independent of order - $ref first", "*"),
                new ("$ref and $dynamicAnchor are independent of order - $defs first", "*"),
                new ("A $dynamicRef without a matching $dynamicAnchor in the same schema resource behaves like a normal $ref to $anchor", "*"),
                new ("A $dynamicRef with a non-matching $dynamicAnchor in the same schema resource behaves like a normal $ref to $anchor", "*")
            };

            return disabledTests.Any(test => test.Item1 == testCaseDescription && (test.Item2 == "*" || test.Item2 == testDescription));
        }
    }
}
