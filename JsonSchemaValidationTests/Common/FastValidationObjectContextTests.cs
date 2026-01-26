// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Tests.Common;

/// <summary>
/// Tests for FastValidationObjectContext - the optimized object context for IsValid() fast path.
/// </summary>
public class FastValidationObjectContextTests
{
    #region Property Tracking Tests

    [Fact]
    public void MarkPropertyEvaluated_SingleProperty_TracksProperty()
    {
        using var doc = JsonDocument.Parse("""{"a": 1, "b": 2, "c": 3}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        context.MarkPropertyEvaluated("a");

        var unevaluated = context.GetUnevaluatedProperties().Select(p => p.Name).ToList();
        Assert.Equal(2, unevaluated.Count);
        Assert.Contains("b", unevaluated);
        Assert.Contains("c", unevaluated);
        Assert.DoesNotContain("a", unevaluated);
    }

    [Fact]
    public void MarkPropertyEvaluated_MultipleProperties_TracksAll()
    {
        using var doc = JsonDocument.Parse("""{"a": 1, "b": 2, "c": 3, "d": 4}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        context.MarkPropertyEvaluated("a");
        context.MarkPropertyEvaluated("c");

        var unevaluated = context.GetUnevaluatedProperties().Select(p => p.Name).ToList();
        Assert.Equal(2, unevaluated.Count);
        Assert.Contains("b", unevaluated);
        Assert.Contains("d", unevaluated);
    }

    [Fact]
    public void MarkPropertyEvaluated_SamePropertyTwice_IsIdempotent()
    {
        using var doc = JsonDocument.Parse("""{"a": 1, "b": 2}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        context.MarkPropertyEvaluated("a");
        context.MarkPropertyEvaluated("a");

        var unevaluated = context.GetUnevaluatedProperties().Select(p => p.Name).ToList();
        Assert.Single(unevaluated);
        Assert.Contains("b", unevaluated);
    }

    [Fact]
    public void MarkPropertyEvaluated_AllProperties_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("""{"a": 1, "b": 2}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        context.MarkPropertyEvaluated("a");
        context.MarkPropertyEvaluated("b");

        var unevaluated = context.GetUnevaluatedProperties().ToList();
        Assert.Empty(unevaluated);
    }

    #endregion

    #region GetUnevaluatedProperties Tests

    [Fact]
    public void GetUnevaluatedProperties_NoneEvaluated_ReturnsAll()
    {
        using var doc = JsonDocument.Parse("""{"x": 1, "y": 2, "z": 3}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        var unevaluated = context.GetUnevaluatedProperties().Select(p => p.Name).ToList();

        Assert.Equal(3, unevaluated.Count);
        Assert.Contains("x", unevaluated);
        Assert.Contains("y", unevaluated);
        Assert.Contains("z", unevaluated);
    }

    [Fact]
    public void GetUnevaluatedProperties_EmptyObject_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("{}");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        var unevaluated = context.GetUnevaluatedProperties().ToList();

        Assert.Empty(unevaluated);
    }

    [Fact]
    public void GetUnevaluatedProperties_NonObjectElement_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        var unevaluated = context.GetUnevaluatedProperties().ToList();

        Assert.Empty(unevaluated);
    }

    [Fact]
    public void GetUnevaluatedProperties_StringElement_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("\"hello\"");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        var unevaluated = context.GetUnevaluatedProperties().ToList();

        Assert.Empty(unevaluated);
    }

    #endregion

    #region SetUnevaluatedPropertiesEvaluated Tests

    [Fact]
    public void SetUnevaluatedPropertiesEvaluated_MarksAllAsEvaluated()
    {
        using var doc = JsonDocument.Parse("""{"a": 1, "b": 2, "c": 3}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        context.SetUnevaluatedPropertiesEvaluated();

        var unevaluated = context.GetUnevaluatedProperties().ToList();
        Assert.Empty(unevaluated);
    }

    [Fact]
    public void SetUnevaluatedPropertiesEvaluated_AfterPartialEvaluation_MarksRemaining()
    {
        using var doc = JsonDocument.Parse("""{"a": 1, "b": 2, "c": 3}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        context.MarkPropertyEvaluated("a");
        context.SetUnevaluatedPropertiesEvaluated();

        var unevaluated = context.GetUnevaluatedProperties().ToList();
        Assert.Empty(unevaluated);
    }

    #endregion

    #region Annotations Tests

    [Fact]
    public void GetAnnotations_ReturnsUnevaluatedProperties()
    {
        using var doc = JsonDocument.Parse("""{"a": 1, "b": 2}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        context.MarkPropertyEvaluated("a");
        var annotations = context.GetAnnotations();

        Assert.Single(annotations.UnEvaluatedProperties);
        Assert.True(annotations.UnEvaluatedProperties.ContainsKey("b"));
    }

    [Fact]
    public void SetAnnotations_MarksPropertiesAsEvaluated()
    {
        using var doc = JsonDocument.Parse("""{"a": 1, "b": 2, "c": 3}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        // Create annotations with only "b" as unevaluated
        var annotations = new JsonValidationObjectContext.Annotations();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name == "b")
            {
                annotations.UnEvaluatedProperties["b"] = prop;
                break;
            }
        }

        context.SetAnnotations(annotations);

        // "a" and "c" should now be evaluated (not in annotations)
        var unevaluated = context.GetUnevaluatedProperties().Select(p => p.Name).ToList();
        Assert.Single(unevaluated);
        Assert.Contains("b", unevaluated);
    }

    [Fact]
    public void SetAnnotations_NonObjectElement_DoesNothing()
    {
        using var doc = JsonDocument.Parse("123");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        var annotations = new JsonValidationObjectContext.Annotations();
        context.SetAnnotations(annotations);

        // Should not throw
        var unevaluated = context.GetUnevaluatedProperties().ToList();
        Assert.Empty(unevaluated);
    }

    #endregion

    #region Context Properties Tests

    [Fact]
    public void Data_ReturnsCorrectElement()
    {
        using var doc = JsonDocument.Parse("""{"test": "value"}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        Assert.Equal(JsonValueKind.Object, context.Data.ValueKind);
    }

    [Fact]
    public void InstanceLocation_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("""{"a": 1}""");
        var context = new FastValidationObjectContext(doc.RootElement, CreateScope());

        Assert.Equal(JsonPointer.Empty, context.InstanceLocation);
    }

    [Fact]
    public void Scope_ReturnsProvidedScope()
    {
        using var doc = JsonDocument.Parse("""{"a": 1}""");
        var scope = CreateScope();
        var context = new FastValidationObjectContext(doc.RootElement, scope);

        Assert.Same(scope, context.Scope);
    }

    #endregion

    #region Helper Methods

    private static IValidationScope CreateScope()
    {
        return new ValidationScope();
    }

    #endregion
}
