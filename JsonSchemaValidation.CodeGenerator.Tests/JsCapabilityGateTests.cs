// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.CodeGeneration.Generator;
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Generator;
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public class JsCapabilityGateTests
{
    private static string? Check(string json, SchemaDraft draft = SchemaDraft.Draft202012)
    {
        var root = JsonDocument.Parse(json).RootElement;
        return JsCapabilityGate.CheckSupported(root, draft);
    }

    [Fact]
    public void Accepts_Self_Contained_Schema()
    {
        var rejection = Check("""{ "type": "string", "minLength": 1 }""");
        Assert.Null(rejection);
    }

    [Fact]
    public void Accepts_Local_Ref()
    {
        var rejection = Check("""
            {
              "$defs": { "s": { "type": "string" } },
              "$ref": "#/$defs/s"
            }
            """);
        Assert.Null(rejection);
    }

    [Fact]
    public void Accepts_Unevaluated_Properties()
    {
        var rejection = Check("""{ "type": "object", "unevaluatedProperties": false }""");
        Assert.Null(rejection);
    }

    [Fact]
    public void Accepts_Unevaluated_Items()
    {
        var rejection = Check("""{ "type": "array", "unevaluatedItems": false }""");
        Assert.Null(rejection);
    }

    [Fact]
    public void Rejects_Dynamic_Ref()
    {
        var rejection = Check("""{ "$dynamicRef": "#meta" }""");
        Assert.NotNull(rejection);
        Assert.Contains("$dynamicRef", rejection);
    }

    [Fact]
    public void Rejects_Dynamic_Anchor()
    {
        var rejection = Check("""{ "$dynamicAnchor": "meta" }""");
        Assert.NotNull(rejection);
        Assert.Contains("$dynamicAnchor", rejection);
    }

    [Fact]
    public void Rejects_Recursive_Ref()
    {
        var rejection = Check("""{ "$recursiveRef": "#" }""", SchemaDraft.Draft202012);
        Assert.NotNull(rejection);
        Assert.Contains("$recursiveRef", rejection);
    }

    [Fact]
    public void Rejects_External_Ref()
    {
        var rejection = Check("""{ "$ref": "https://example.com/other.json" }""");
        Assert.NotNull(rejection);
        Assert.Contains("external $ref", rejection);
    }

    [Fact]
    public void Rejects_External_Ref_Nested()
    {
        var rejection = Check("""
            {
              "type": "object",
              "properties": {
                "inner": { "$ref": "./sibling.json" }
              }
            }
            """);
        Assert.NotNull(rejection);
        Assert.Contains("external $ref", rejection);
    }

    [Fact]
    public void Rejects_Unsupported_Draft()
    {
        var rejection = Check("""{ "type": "string" }""", SchemaDraft.Draft7);
        Assert.NotNull(rejection);
        Assert.Contains("Draft 4 and Draft 2020-12", rejection);
    }

    [Fact]
    public void Accepts_Draft_4()
    {
        var rejection = Check("""{ "type": "string" }""", SchemaDraft.Draft4);
        Assert.Null(rejection);
    }
}
