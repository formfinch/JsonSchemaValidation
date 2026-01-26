// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.Tests.Common;

/// <summary>
/// Tests for JsonPointer RFC 6901 implementation.
/// </summary>
public class JsonPointerTests
{
    #region Empty Pointer

    [Fact]
    public void Empty_ReturnsEmptyString()
    {
        var pointer = JsonPointer.Empty;

        Assert.Equal("", pointer.ToString());
    }

    #endregion

    #region Parse

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var pointer = JsonPointer.Parse("");

        Assert.Equal("", pointer.ToString());
    }

    [Fact]
    public void Parse_SingleSegment_ReturnsCorrectPointer()
    {
        var pointer = JsonPointer.Parse("/foo");

        Assert.Equal("/foo", pointer.ToString());
    }

    [Fact]
    public void Parse_MultipleSegments_ReturnsCorrectPointer()
    {
        var pointer = JsonPointer.Parse("/foo/bar/baz");

        Assert.Equal("/foo/bar/baz", pointer.ToString());
    }

    [Fact]
    public void Parse_ArrayIndex_ReturnsCorrectPointer()
    {
        var pointer = JsonPointer.Parse("/items/0");

        Assert.Equal("/items/0", pointer.ToString());
    }

    [Fact]
    public void Parse_EscapedTilde_DecodesCorrectly()
    {
        var pointer = JsonPointer.Parse("/a~0b");
        // When re-serialized, ~ becomes ~0
        Assert.Equal("/a~0b", pointer.ToString());
    }

    [Fact]
    public void Parse_EscapedSlash_DecodesCorrectly()
    {
        var pointer = JsonPointer.Parse("/a~1b");
        // When re-serialized, / becomes ~1
        Assert.Equal("/a~1b", pointer.ToString());
    }

    [Fact]
    public void Parse_EmptySegment_Preserved()
    {
        var pointer = JsonPointer.Parse("/");

        Assert.Equal("/", pointer.ToString());
    }

    [Fact]
    public void Parse_InvalidPointer_ThrowsException()
    {
        // Pointer must start with / or be empty
        Assert.Throws<ArgumentException>(() => JsonPointer.Parse("foo"));
    }

    [Fact]
    public void Parse_Null_ReturnsEmpty()
    {
        var pointer = JsonPointer.Parse(null!);

        Assert.Equal("", pointer.ToString());
    }

    #endregion

    #region Append String

    [Fact]
    public void Append_ToEmpty_CreatesNewPointer()
    {
        var pointer = JsonPointer.Empty.Append("foo");

        Assert.Equal("/foo", pointer.ToString());
    }

    [Fact]
    public void Append_MultipleSegments_ChainsCorrectly()
    {
        var pointer = JsonPointer.Empty
            .Append("foo")
            .Append("bar")
            .Append("baz");

        Assert.Equal("/foo/bar/baz", pointer.ToString());
    }

    [Fact]
    public void Append_SpecialCharacterTilde_EscapesCorrectly()
    {
        var pointer = JsonPointer.Empty.Append("a~b");

        Assert.Equal("/a~0b", pointer.ToString());
    }

    [Fact]
    public void Append_SpecialCharacterSlash_EscapesCorrectly()
    {
        var pointer = JsonPointer.Empty.Append("a/b");

        Assert.Equal("/a~1b", pointer.ToString());
    }

    [Fact]
    public void Append_MixedSpecialCharacters_EscapesCorrectly()
    {
        var pointer = JsonPointer.Empty.Append("~/");

        Assert.Equal("/~0~1", pointer.ToString());
    }

    #endregion

    #region Append Int

    [Fact]
    public void AppendInt_AddsNumericSegment()
    {
        var pointer = JsonPointer.Empty.Append("items").Append(5);

        Assert.Equal("/items/5", pointer.ToString());
    }

    [Fact]
    public void AppendInt_Zero_Works()
    {
        var pointer = JsonPointer.Empty.Append(0);

        Assert.Equal("/0", pointer.ToString());
    }

    [Fact]
    public void AppendInt_LargeNumber_Works()
    {
        var pointer = JsonPointer.Empty.Append(12345);

        Assert.Equal("/12345", pointer.ToString());
    }

    #endregion

    #region Parent

    [Fact]
    public void Parent_SingleSegment_ReturnsEmpty()
    {
        var pointer = JsonPointer.Parse("/foo");
        var parent = pointer.Parent();

        Assert.Equal("", parent.ToString());
    }

    [Fact]
    public void Parent_MultipleSegments_ReturnsParent()
    {
        var pointer = JsonPointer.Parse("/foo/bar/baz");
        var parent = pointer.Parent();

        Assert.Equal("/foo/bar", parent.ToString());
    }

    [Fact]
    public void Parent_Empty_ReturnsEmpty()
    {
        var parent = JsonPointer.Empty.Parent();

        Assert.Equal("", parent.ToString());
    }

    [Fact]
    public void Parent_TwoSegments_ReturnsSingleSegment()
    {
        var pointer = JsonPointer.Parse("/foo/bar");
        var parent = pointer.Parent();

        Assert.Equal("/foo", parent.ToString());
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SamePointer_ReturnsTrue()
    {
        var pointer1 = JsonPointer.Parse("/foo/bar");
        var pointer2 = JsonPointer.Parse("/foo/bar");

        Assert.True(pointer1.Equals(pointer2));
    }

    [Fact]
    public void Equals_DifferentPointer_ReturnsFalse()
    {
        var pointer1 = JsonPointer.Parse("/foo/bar");
        var pointer2 = JsonPointer.Parse("/foo/baz");

        Assert.False(pointer1.Equals(pointer2));
    }

    [Fact]
    public void Equals_EmptyPointers_ReturnsTrue()
    {
        var pointer1 = JsonPointer.Empty;
        var pointer2 = JsonPointer.Parse("");

        Assert.True(pointer1.Equals(pointer2));
    }

    [Fact]
    public void Equals_NonJsonPointer_ReturnsFalse()
    {
        var pointer = JsonPointer.Parse("/foo");

        Assert.False(pointer.Equals("not a pointer"));
        Assert.False(pointer.Equals(null));
    }

    [Fact]
    public void GetHashCode_SamePointers_SameHash()
    {
        var pointer1 = JsonPointer.Parse("/foo/bar");
        var pointer2 = JsonPointer.Parse("/foo/bar");

        Assert.Equal(pointer1.GetHashCode(), pointer2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentPointers_DifferentHash()
    {
        var pointer1 = JsonPointer.Parse("/foo");
        var pointer2 = JsonPointer.Parse("/bar");

        // Hash codes can technically collide, but these simple cases should differ
        Assert.NotEqual(pointer1.GetHashCode(), pointer2.GetHashCode());
    }

    #endregion

    #region ToString Caching

    [Fact]
    public void ToString_CalledMultipleTimes_ReturnsSameInstance()
    {
        var pointer = JsonPointer.Parse("/foo/bar");

        var str1 = pointer.ToString();
        var str2 = pointer.ToString();

        // Should be the same cached instance
        Assert.Same(str1, str2);
    }

    #endregion

    #region Round-trip

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    [InlineData("/foo/0/bar")]
    [InlineData("/a~0b")]
    [InlineData("/a~1b")]
    [InlineData("/~0~1")]
    public void Parse_ToString_RoundTrips(string input)
    {
        var pointer = JsonPointer.Parse(input);
        var output = pointer.ToString();

        Assert.Equal(input, output);
    }

    #endregion
}
