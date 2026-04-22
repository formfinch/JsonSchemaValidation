// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using Xunit;

namespace FormFinch.JsonSchemaValidation.CodeGenerator.Tests;

public class JsMetaSchemaBehaviorTests
{
    [Fact]
    public void CustomMetaschema_WithoutValidationVocabulary_DisablesValidationKeywords()
    {
        var harness = new JsValidatorHarness(
            externalSchemaDocuments: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["http://localhost:1234/draft2020-12/metaschema-no-validation.json"] =
                    """
                    {
                      "$schema": "https://json-schema.org/draft/2020-12/schema",
                      "$id": "http://localhost:1234/draft2020-12/metaschema-no-validation.json",
                      "$vocabulary": {
                        "https://json-schema.org/draft/2020-12/vocab/applicator": true,
                        "https://json-schema.org/draft/2020-12/vocab/core": true
                      },
                      "$dynamicAnchor": "meta",
                      "allOf": [
                        { "$ref": "https://json-schema.org/draft/2020-12/meta/applicator" },
                        { "$ref": "https://json-schema.org/draft/2020-12/meta/core" }
                      ]
                    }
                    """
            });

        var result = harness.Evaluate(
            """
            {
              "$schema": "http://localhost:1234/draft2020-12/metaschema-no-validation.json",
              "properties": {
                "badProperty": false,
                "numberProperty": { "minimum": 10 }
              }
            }
            """,
            [
                """{"badProperty":"boom"}""",
                """{"numberProperty":1}""",
                """{"numberProperty":20}""",
            ]);

        Assert.True(result.Success, result.Error);
        Assert.Equal([false, true, true], result.Verdicts);
    }

    [Fact]
    public void CustomMetaschema_WithFormatAssertionVocabulary_EnablesFormatAssertion()
    {
        var harness = new JsValidatorHarness(
            externalSchemaDocuments: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["http://localhost:1234/draft2020-12/format-assertion-false.json"] =
                    """
                    {
                      "$id": "http://localhost:1234/draft2020-12/format-assertion-false.json",
                      "$schema": "https://json-schema.org/draft/2020-12/schema",
                      "$vocabulary": {
                        "https://json-schema.org/draft/2020-12/vocab/core": true,
                        "https://json-schema.org/draft/2020-12/vocab/format-assertion": false
                      },
                      "$dynamicAnchor": "meta",
                      "allOf": [
                        { "$ref": "https://json-schema.org/draft/2020-12/meta/core" },
                        { "$ref": "https://json-schema.org/draft/2020-12/meta/format-assertion" }
                      ]
                    }
                    """
            });

        var result = harness.Evaluate(
            """
            {
              "$schema": "http://localhost:1234/draft2020-12/format-assertion-false.json",
              "format": "ipv4"
            }
            """,
            [
                "\"127.0.0.1\"",
                "\"not-an-ipv4\"",
            ]);

        Assert.True(result.Success, result.Error);
        Assert.Equal([true, false], result.Verdicts);
    }
}
