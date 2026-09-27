// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.CSharp.Generator;
using FormFinch.JsonSchemaValidation.Compiler;
using FormFinch.JsonSchemaValidation.Formats;

namespace FormFinch.JsonSchemaValidationTests.Formats;

/// <summary>
/// Runs the extended format test vectors through compiled C# validators, generated with the
/// extended catalog on and off.
/// </summary>
public class CompiledExtendedFormatTests
{
    // Compiled validators are cached per schema, so one factory per mode keeps the suite fast.
    private static readonly RuntimeValidatorFactory WithCatalog = new(registry: null, extendedFormats: true);
    private static readonly RuntimeValidatorFactory WithoutCatalog = new(registry: null);

    [Theory]
    [MemberData(nameof(ExtendedFormatTests.GetVectors), MemberType = typeof(ExtendedFormatTests))]
    public void Vector_CompiledWithCatalog_MatchesExpectation(string file, string description, string schema, string data, bool valid)
    {
        var validator = WithCatalog.Compile(schema);
        using var instance = JsonDocument.Parse(data);

        Assert.True(valid == validator.IsValid(instance.RootElement), $"{file}: {description} ({data}) expected valid={valid}");
    }

    [Theory]
    [MemberData(nameof(ExtendedFormatTests.GetVectors), MemberType = typeof(ExtendedFormatTests))]
    public void Vector_CompiledWithoutCatalog_IsAnnotationOnly(string file, string description, string schema, string data, bool valid)
    {
        _ = valid;
        var validator = WithoutCatalog.Compile(schema);
        using var instance = JsonDocument.Parse(data);

        Assert.True(validator.IsValid(instance.RootElement), $"{file}: {description} ({data}) should pass while the catalog is off");
    }

    [Fact]
    public void GeneratedCode_CallsCatalogOnlyWhenEnabled()
    {
        using var schema = JsonDocument.Parse("""{"$schema": "https://json-schema.org/draft/2020-12/schema", "format": "nl-phone"}""");

        var on = new CSharpSchemaCodeGenerator { ExtendedFormats = true }.Generate(schema.RootElement, "Generated", "PhoneOn");
        var off = new CSharpSchemaCodeGenerator().Generate(schema.RootElement, "Generated", "PhoneOff");

        Assert.True(on.Success, on.Error);
        Assert.True(off.Success, off.Error);
        Assert.Contains("global::FormFinch.JsonSchemaValidation.Formats.ExtendedFormats.IsValid(\"nl-phone\"", on.GeneratedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("ExtendedFormats", off.GeneratedCode, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltInFormat_IsUnaffectedByCatalogFlag()
    {
        using var schema = JsonDocument.Parse("""{"$schema": "https://json-schema.org/draft/2020-12/schema", "format": "email"}""");

        var code = new CSharpSchemaCodeGenerator { ExtendedFormats = true }.Generate(schema.RootElement, "Generated", "Email").GeneratedCode;

        Assert.Contains("FormatValidators.IsValidEmail", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ExtendedFormats", code, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownFormat_StaysAnnotationWithCatalogOn()
    {
        var validator = WithCatalog.Compile("""{"$schema": "https://json-schema.org/draft/2020-12/schema", "format": "no-such-format"}""");
        using var instance = JsonDocument.Parse("\"anything\"");

        Assert.True(validator.IsValid(instance.RootElement));
    }

    [Fact]
    public void EntryPoint_IgnoresNonStringsAndRejectsUnknownNames()
    {
        using var number = JsonDocument.Parse("12345");
        using var text = JsonDocument.Parse("\"06-12345678\"");

        Assert.True(ExtendedFormats.IsValid("nl-phone", number.RootElement));
        Assert.True(ExtendedFormats.IsValid("nl-phone", text.RootElement));
        Assert.Throws<ArgumentException>(() => ExtendedFormats.IsValid("no-such-format", text.RootElement));
    }
}
