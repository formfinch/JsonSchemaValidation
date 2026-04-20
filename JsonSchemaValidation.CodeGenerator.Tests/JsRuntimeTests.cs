// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public class JsRuntimeTests
{
    [Fact]
    public void GetSource_ReturnsNonEmpty()
    {
        var source = JsRuntime.GetSource();
        Assert.NotNull(source);
        Assert.NotEmpty(source);
    }

    [Theory]
    [InlineData("export function escapeJsonPointer")]
    [InlineData("export function graphemeLength")]
    [InlineData("export function isInteger")]
    [InlineData("export function deepEquals")]
    public void GetSource_ExposesFrozenHelpers(string expectedFragment)
    {
        var source = JsRuntime.GetSource();
        Assert.Contains(expectedFragment, source);
    }

    [Theory]
    [InlineData("isValidDateTime")]
    [InlineData("isValidDate")]
    [InlineData("isValidTime")]
    [InlineData("isValidDuration")]
    [InlineData("isValidEmail")]
    [InlineData("isValidIdnEmail")]
    [InlineData("isValidHostname")]
    [InlineData("isValidIdnHostname")]
    [InlineData("isValidIpv4")]
    [InlineData("isValidIpv6")]
    [InlineData("isValidUri")]
    [InlineData("isValidUriReference")]
    [InlineData("isValidUriTemplate")]
    [InlineData("isValidIri")]
    [InlineData("isValidIriReference")]
    [InlineData("isValidJsonPointer")]
    [InlineData("isValidRelativeJsonPointer")]
    [InlineData("isValidRegex")]
    [InlineData("isValidUuid")]
    public void GetSource_ExposesAllFrozenFormatNames(string formatExport)
    {
        var source = JsRuntime.GetSource();
        // Format exports can be `export function name` or `export const name = ...` (for aliases).
        var asFunction = source.Contains($"export function {formatExport}");
        var asConst = source.Contains($"export const {formatExport}");
        Assert.True(asFunction || asConst, $"Runtime does not export '{formatExport}' as function or const.");
    }

    [Fact]
    public void GetSource_StampsAbiVersion()
    {
        var source = JsRuntime.GetSource();
        Assert.Contains("ABI VERSION: mvp-0", source);
    }
}
