// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidationTests.Common;

/// <summary>
/// Tests for the <see cref="LruCache{TKey, TValue}"/> class.
/// </summary>
public class LruCacheTests
{
    [Fact]
    public void Set_WithinCapacity_AddsAllEntries()
    {
        var cache = new LruCache<string, int>(3);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        Assert.Equal(3, cache.Count);
        Assert.True(cache.TryGetValue("a", out var a) && a == 1);
        Assert.True(cache.TryGetValue("b", out var b) && b == 2);
        Assert.True(cache.TryGetValue("c", out var c) && c == 3);
    }

    [Fact]
    public void Set_ExceedsCapacity_EvictsLeastRecentlyUsed()
    {
        var cache = new LruCache<string, int>(3);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);
        cache.Set("d", 4); // Should evict "a" (oldest)

        Assert.Equal(3, cache.Count);
        Assert.False(cache.TryGetValue("a", out _)); // Evicted
        Assert.True(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out _));
        Assert.True(cache.TryGetValue("d", out _));
    }

    [Fact]
    public void TryGetValue_TouchesEntry_PreventsEviction()
    {
        var cache = new LruCache<string, int>(3);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        // Touch "a" to make it most recently used
        Assert.True(cache.TryGetValue("a", out _));

        cache.Set("d", 4); // Should evict "b" (now oldest)

        Assert.Equal(3, cache.Count);
        Assert.True(cache.TryGetValue("a", out _));  // Was touched, still present
        Assert.False(cache.TryGetValue("b", out _)); // Evicted (was oldest after "a" was touched)
        Assert.True(cache.TryGetValue("c", out _));
        Assert.True(cache.TryGetValue("d", out _));
    }

    [Fact]
    public void Set_UpdateExisting_MovesToMostRecent()
    {
        var cache = new LruCache<string, int>(3);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        // Update "a" to make it most recently used
        cache.Set("a", 10);

        cache.Set("d", 4); // Should evict "b" (now oldest)

        Assert.Equal(3, cache.Count);
        Assert.True(cache.TryGetValue("a", out var a) && a == 10); // Updated and present
        Assert.False(cache.TryGetValue("b", out _)); // Evicted
        Assert.True(cache.TryGetValue("c", out _));
        Assert.True(cache.TryGetValue("d", out _));
    }

    [Fact]
    public void TryGetValue_NonExistentKey_ReturnsFalse()
    {
        var cache = new LruCache<string, int>(3);

        cache.Set("a", 1);

        Assert.False(cache.TryGetValue("nonexistent", out var value));
        Assert.Equal(default, value);
    }

    [Fact]
    public void EvictionOrder_FollowsLruPattern()
    {
        var cache = new LruCache<string, int>(3);

        // Add in order: a, b, c
        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);

        // Access in order: b, a (c is now LRU)
        cache.TryGetValue("b", out _);
        cache.TryGetValue("a", out _);

        // Add d - should evict c (LRU)
        cache.Set("d", 4);
        Assert.False(cache.TryGetValue("c", out _));

        // Add e - should evict b (now LRU after c evicted)
        cache.Set("e", 5);
        Assert.False(cache.TryGetValue("b", out _));

        // Remaining: a, d, e
        Assert.True(cache.TryGetValue("a", out _));
        Assert.True(cache.TryGetValue("d", out _));
        Assert.True(cache.TryGetValue("e", out _));
    }

    [Fact]
    public void Constructor_WithComparer_UsesComparer()
    {
        var cache = new LruCache<string, int>(3, StringComparer.OrdinalIgnoreCase);

        cache.Set("ABC", 1);

        Assert.True(cache.TryGetValue("abc", out var value));
        Assert.Equal(1, value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_InvalidCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<string, int>(capacity));
    }

    [Fact]
    public void Set_CapacityOne_EvictsOnEachAdd()
    {
        var cache = new LruCache<string, int>(1);

        cache.Set("a", 1);
        Assert.Equal(1, cache.Count);

        cache.Set("b", 2);
        Assert.Equal(1, cache.Count);
        Assert.False(cache.TryGetValue("a", out _));
        Assert.True(cache.TryGetValue("b", out var b) && b == 2);

        cache.Set("c", 3);
        Assert.Equal(1, cache.Count);
        Assert.False(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out var c) && c == 3);
    }
}
