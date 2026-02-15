// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.Tests.Common;

/// <summary>
/// Tests for FastValidationArrayContext - the optimized array context for IsValid() fast path.
/// </summary>
public class FastValidationArrayContextTests
{
    #region Index Tracking Tests

    [Fact]
    public void SetEvaluatedIndex_ContiguousTracking_TracksMaxIndex()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3, 4, 5]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetEvaluatedIndex(0);
        context.SetEvaluatedIndex(1);
        context.SetEvaluatedIndex(2);

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Equal(2, unevaluated.Count); // items 3, 4 (indices 3, 4)
    }

    [Fact]
    public void SetEvaluatedIndex_OutOfOrder_TracksMaxValue()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3, 4, 5]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetEvaluatedIndex(2);
        context.SetEvaluatedIndex(0);
        context.SetEvaluatedIndex(1);

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Equal(2, unevaluated.Count); // items at indices 3, 4
    }

    [Fact]
    public void SetEvaluatedIndex_SameValueMultipleTimes_IsIdempotent()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetEvaluatedIndex(1);
        context.SetEvaluatedIndex(1);
        context.SetEvaluatedIndex(1);

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Single(unevaluated); // only index 2 is unevaluated
    }

    #endregion

    #region Indices Set Tracking Tests

    [Fact]
    public void SetEvaluatedIndices_NonContiguous_TracksAllIndices()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3, 4, 5]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetEvaluatedIndices(new[] { 0, 2, 4 });

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Equal(2, unevaluated.Count); // indices 1, 3
    }

    [Fact]
    public void SetEvaluatedIndices_EmptyEnumerable_NoChange()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetEvaluatedIndices(Array.Empty<int>());

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Equal(3, unevaluated.Count); // all items still unevaluated
    }

    [Fact]
    public void SetEvaluatedIndices_CombinedWithSetEvaluatedIndex_MergesCorrectly()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3, 4, 5]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetEvaluatedIndex(0); // evaluates 0
        context.SetEvaluatedIndices(new[] { 2, 4 }); // evaluates 2, 4

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Equal(2, unevaluated.Count); // indices 1, 3
    }

    #endregion

    #region Unevaluated Items Tests

    [Fact]
    public void GetUnevaluatedItems_AllEvaluated_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetEvaluatedIndex(2); // 0, 1, 2 all covered

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Empty(unevaluated);
    }

    [Fact]
    public void GetUnevaluatedItems_NoneEvaluated_ReturnsAll()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3, 4, 5]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Equal(5, unevaluated.Count);
    }

    [Fact]
    public void GetUnevaluatedItems_EmptyArray_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("[]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Empty(unevaluated);
    }

    [Fact]
    public void GetUnevaluatedItems_NonArrayElement_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("{}");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Empty(unevaluated);
    }

    #endregion

    #region Flag State Tests

    [Fact]
    public void SetAllItemsEvaluated_MarksAllAsEvaluated()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetAllItemsEvaluated();

        var unevaluated = context.GetUnevaluatedItems().ToList();
        Assert.Empty(unevaluated);
    }

    [Fact]
    public void SetAdditionalItemsEvaluated_SetsFlag()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetAdditonalItemsEvaluated();

        var annotations = context.GetAnnotations();
        Assert.True(annotations.AdditionalItemsEvaluated);
    }

    [Fact]
    public void SetUnevaluatedItemsEvaluated_SetsFlag()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetUnevaluatedItemsEvaluated();

        var annotations = context.GetAnnotations();
        Assert.True(annotations.UnevaluatedItemsEvaluated);
    }

    [Fact]
    public void MultipleFlags_CanBeSetIndependently()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetAdditonalItemsEvaluated();
        context.SetUnevaluatedItemsEvaluated();
        context.SetAllItemsEvaluated();

        var annotations = context.GetAnnotations();
        Assert.True(annotations.AdditionalItemsEvaluated);
        Assert.True(annotations.UnevaluatedItemsEvaluated);
        Assert.True(annotations.ItemsEvaluated);
    }

    #endregion

    #region Annotation Persistence Tests

    [Fact]
    public void GetAnnotations_SetAnnotations_RoundTrips()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3, 4, 5]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        context.SetEvaluatedIndex(2);
        context.SetEvaluatedIndices(new[] { 4 });
        context.SetAdditonalItemsEvaluated();

        var annotations = context.GetAnnotations();

        // Create new context and restore
        var context2 = new FastValidationArrayContext(doc.RootElement, CreateScope());
        context2.SetAnnotations(annotations);

        var unevaluated1 = context.GetUnevaluatedItems().ToList();
        var unevaluated2 = context2.GetUnevaluatedItems().ToList();

        Assert.Equal(unevaluated1.Count, unevaluated2.Count);
    }

    [Fact]
    public void EmptyAnnotations_CanBeCapturedAndRestored()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        var annotations = context.GetAnnotations();

        var context2 = new FastValidationArrayContext(doc.RootElement, CreateScope());
        context2.SetAnnotations(annotations);

        var unevaluated = context2.GetUnevaluatedItems().ToList();
        Assert.Equal(3, unevaluated.Count); // all still unevaluated
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Data_ReturnsCorrectElement()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        Assert.Equal(JsonValueKind.Array, context.Data.ValueKind);
        Assert.Equal(3, context.Data.GetArrayLength());
    }

    [Fact]
    public void InstanceLocation_ReturnsEmpty()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var context = new FastValidationArrayContext(doc.RootElement, CreateScope());

        Assert.Equal(JsonPointer.Empty, context.InstanceLocation);
    }

    [Fact]
    public void Scope_ReturnsProvidedScope()
    {
        using var doc = JsonDocument.Parse("[1, 2, 3]");
        var scope = CreateScope();
        var context = new FastValidationArrayContext(doc.RootElement, scope);

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
