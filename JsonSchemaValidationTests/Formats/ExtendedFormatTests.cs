// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Formats;
using FormFinch.JsonSchemaValidationTests.TestCases;

namespace FormFinch.JsonSchemaValidationTests.Formats;

/// <summary>
/// Runs the extended format test vectors (<c>TestData/Formats</c>, JSON-Schema-Test-Suite shape)
/// through the runtime validator, with the catalog on and off.
/// </summary>
public class ExtendedFormatTests
{
    private static readonly string TestDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Formats");

    public static IEnumerable<object[]> GetVectors()
    {
        foreach (var file in Directory.GetFiles(TestDataPath, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            var cases = JsonSerializer.Deserialize<List<TestCase>>(File.ReadAllText(file))!;
            foreach (var testCase in cases)
            {
                foreach (var test in testCase.Tests)
                {
                    yield return new object[]
                    {
                        Path.GetFileNameWithoutExtension(file),
                        test.GetProperty("description").GetString()!,
                        testCase.Schema.GetRawText(),
                        test.GetProperty("data").GetRawText(),
                        test.GetProperty("valid").GetBoolean(),
                    };
                }
            }
        }
    }

    private static SchemaValidationOptions Options(bool extended, bool assertion)
    {
        var options = new SchemaValidationOptions { EnableExtendedFormats = extended };
        options.Draft202012.FormatAssertionEnabled = assertion;
        return options;
    }

    [Theory]
    [MemberData(nameof(GetVectors))]
    public void Vector_WithCatalogAndAssertion_MatchesExpectation(string file, string description, string schema, string data, bool valid)
    {
        var result = JsonSchemaValidator.Validate(schema, data, Options(extended: true, assertion: true));

        Assert.True(valid == result.Valid, $"{file}: {description} ({data}) expected valid={valid}");
    }

    [Theory]
    [MemberData(nameof(GetVectors))]
    public void Vector_WithCatalogDisabled_IsAnnotationOnly(string file, string description, string schema, string data, bool valid)
    {
        _ = valid;
        var result = JsonSchemaValidator.Validate(schema, data, Options(extended: false, assertion: true));

        Assert.True(result.Valid, $"{file}: {description} ({data}) should pass while the catalog is disabled");
    }

    [Theory]
    [MemberData(nameof(GetVectors))]
    public void Vector_WithoutFormatAssertion_IsAnnotationOnly(string file, string description, string schema, string data, bool valid)
    {
        _ = valid;
        var result = JsonSchemaValidator.Validate(schema, data, Options(extended: true, assertion: false));

        Assert.True(result.Valid, $"{file}: {description} ({data}) should pass while format assertion is off");
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-03/schema#")]
    [InlineData("http://json-schema.org/draft-04/schema#")]
    [InlineData("http://json-schema.org/draft-06/schema#")]
    [InlineData("http://json-schema.org/draft-07/schema#")]
    [InlineData("https://json-schema.org/draft/2019-09/schema")]
    [InlineData("https://json-schema.org/draft/2020-12/schema")]
    public void EveryDraft_AssertsCatalogFormats(string draft)
    {
        var options = new SchemaValidationOptions { EnableExtendedFormats = true };
        options.Draft3.FormatAssertionEnabled = true;
        options.Draft4.FormatAssertionEnabled = true;
        options.Draft6.FormatAssertionEnabled = true;
        options.Draft7.FormatAssertionEnabled = true;
        options.Draft201909.FormatAssertionEnabled = true;
        options.Draft202012.FormatAssertionEnabled = true;
        var schema = $$"""{"$schema": "{{draft}}", "format": "iban"}""";

        Assert.True(JsonSchemaValidator.Validate(schema, "\"NL91 ABNA 0417 1643 00\"", options).Valid);
        Assert.False(JsonSchemaValidator.Validate(schema, "\"NL91 ABNA 0417 1643 01\"", options).Valid);
    }

    [Fact]
    public void FailedFormat_ReportsFormatName()
    {
        var result = JsonSchemaValidator.Validate(
            """{"format": "nl-bsn"}""",
            "\"123456789\"",
            Options(extended: true, assertion: true));

        Assert.False(result.Valid);
        Assert.Contains(result.Errors!, e => e.Error == "Value is not a valid nl-bsn");
    }

    [Fact]
    public void Vectors_CoverEveryCatalogFormat()
    {
        var files = Directory.GetFiles(TestDataPath, "*.json").Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.Ordinal);

        Assert.All(ExtendedFormatValidators.FormatNames, name => Assert.Contains(name, files));
    }

    [Fact]
    public void IsValid_ByName_UsesTheCatalog()
    {
        Assert.True(ExtendedFormatValidators.IsKnownFormat("nl-postcode"));
        Assert.False(ExtendedFormatValidators.IsKnownFormat("email"));
        Assert.True(ExtendedFormatValidators.IsValid("nl-postcode", "1234 ab"));
        Assert.False(ExtendedFormatValidators.IsValid("nl-postcode", "1234 sa"));
        Assert.Throws<ArgumentException>(() => ExtendedFormatValidators.IsValid("no-such-format", "x"));
    }
}
