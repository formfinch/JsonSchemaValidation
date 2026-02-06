// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Stress.Memory;

/// <summary>
/// Memory leak detection tests using WeakReference and bounded-memory patterns.
/// Verifies that objects are properly garbage-collected and memory stays bounded.
/// </summary>
[Trait("Category", "Stress")]
public class MemoryLeakTests
{
    #region LruCache Tests

    [Fact]
    public void LruCache_EvictedItems_AreCollectible()
    {
        var cache = new LruCache<int, object>(2);
        var weakRefs = new WeakReference[3];

        // Fill cache beyond capacity so item 0 gets evicted
        for (int i = 0; i < 3; i++)
        {
            var value = new byte[1024];
            weakRefs[i] = new WeakReference(value);
            cache.Set(i, value);
        }

        // Item 0 was evicted when item 2 was added (capacity is 2)
        Assert.False(cache.TryGetValue(0, out _));

        // Force collection
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        // Evicted item should be collectible
        Assert.False(weakRefs[0].IsAlive);

        // Items still in cache should be alive
        Assert.True(weakRefs[1].IsAlive);
        Assert.True(weakRefs[2].IsAlive);
    }

    [Fact]
    public void LruCache_RepeatedOperations_BoundedMemory()
    {
        const int capacity = 100;
        const int iterations = 50_000;
        var cache = new LruCache<int, byte[]>(capacity);

        // Warm up: fill cache and let GC settle
        for (int i = 0; i < capacity; i++)
        {
            cache.Set(i, new byte[256]);
        }

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var baselineMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Perform many set operations that cause continuous eviction
        for (int i = capacity; i < capacity + iterations; i++)
        {
            cache.Set(i, new byte[256]);
        }

        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
        var growth = finalMemory - baselineMemory;

        // Memory should not grow significantly beyond baseline.
        // Cache holds 100 entries * 256 bytes = ~25KB of values.
        // Allow generous 2MB margin for GC overhead / timing.
        Assert.True(growth < 2 * 1024 * 1024,
            $"Memory grew by {growth:N0} bytes after {iterations:N0} operations (baseline: {baselineMemory:N0}, final: {finalMemory:N0})");
        Assert.Equal(capacity, cache.Count);
    }

    #endregion

    #region SchemaRepository Tests

    [Fact]
    public void SchemaRepository_DisposedServiceProvider_IsCollectible()
    {
        var weakSp = CreateAndDisposeServiceProvider();

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        Assert.False(weakSp.IsAlive,
            "ServiceProvider should be collectible after disposal");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt => opt.EnableDraft202012 = true);
        var sp = services.BuildServiceProvider();
        sp.InitializeSingletonServices();

        // Use the repository so it's actually initialized
        var repo = sp.GetRequiredService<ISchemaRepository>();
        repo.TryRegisterSchema(
            System.Text.Json.JsonDocument.Parse("""{"type": "string"}""").RootElement,
            out _);

        var weakRef = new WeakReference(sp);
        sp.Dispose();
        return weakRef;
    }

    #endregion

    #region Static API — IsValid (Fast Path) Tests

    [Fact]
    public void StaticApi_RepeatedIsValid_BoundedMemory()
    {
        const int iterations = 10_000;
        const string schema = """{"type": "integer", "minimum": 0}""";

        // Warm up: let static lazy init + cache settle
        JsonSchemaValidator.IsValid(schema, "42");
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var baselineMemory = GC.GetTotalMemory(forceFullCollection: true);

        for (int i = 0; i < iterations; i++)
        {
            JsonSchemaValidator.IsValid(schema, "42");
        }

        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
        var growth = finalMemory - baselineMemory;

        Assert.True(growth < 2 * 1024 * 1024,
            $"IsValid: Memory grew by {growth:N0} bytes after {iterations:N0} iterations (baseline: {baselineMemory:N0}, final: {finalMemory:N0})");
    }

    [Fact]
    public void StaticApi_ManyUniqueSchemas_IsValid_BoundedByLruCache()
    {
        const int uniqueSchemas = 2000;

        // Warm up static services
        JsonSchemaValidator.IsValid("""{"type": "string"}""", "\"x\"");
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var baselineMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Generate 2000 unique schemas (LRU cache capacity is 1000)
        for (int i = 0; i < uniqueSchemas; i++)
        {
            var schema = $$"""{"type": "integer", "minimum": {{i}}}""";
            JsonSchemaValidator.IsValid(schema, "42");
        }

        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
        var growth = finalMemory - baselineMemory;

        // With LRU cache of 1000, memory should be bounded.
        // SchemaRepository grows (it doesn't evict), but each schema is small.
        // Allow 20MB — generous margin for 2000 tiny schemas in the repository.
        Assert.True(growth < 20 * 1024 * 1024,
            $"IsValid unique schemas: Memory grew by {growth:N0} bytes for {uniqueSchemas} schemas (baseline: {baselineMemory:N0}, final: {finalMemory:N0})");
    }

    #endregion

    #region Static API — Validate (Full Output Path) Tests

    [Fact]
    public void StaticApi_RepeatedValidate_BoundedMemory()
    {
        const int iterations = 10_000;
        const string schema = """{"type": "integer", "minimum": 0}""";

        // Warm up
        JsonSchemaValidator.Validate(schema, "42");
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var baselineMemory = GC.GetTotalMemory(forceFullCollection: true);

        for (int i = 0; i < iterations; i++)
        {
            JsonSchemaValidator.Validate(schema, "42");
        }

        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
        var growth = finalMemory - baselineMemory;

        Assert.True(growth < 2 * 1024 * 1024,
            $"Validate: Memory grew by {growth:N0} bytes after {iterations:N0} iterations (baseline: {baselineMemory:N0}, final: {finalMemory:N0})");
    }

    [Fact]
    public void StaticApi_ManyUniqueSchemas_Validate_BoundedByLruCache()
    {
        const int uniqueSchemas = 2000;

        // Warm up static services
        JsonSchemaValidator.Validate("""{"type": "string"}""", "\"x\"");
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        var baselineMemory = GC.GetTotalMemory(forceFullCollection: true);

        for (int i = 0; i < uniqueSchemas; i++)
        {
            var schema = $$"""{"type": "integer", "minimum": {{i}}}""";
            JsonSchemaValidator.Validate(schema, "42");
        }

        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
        var growth = finalMemory - baselineMemory;

        Assert.True(growth < 20 * 1024 * 1024,
            $"Validate unique schemas: Memory grew by {growth:N0} bytes for {uniqueSchemas} schemas (baseline: {baselineMemory:N0}, final: {finalMemory:N0})");
    }

    #endregion

    #region Object Lifetime Tests

    [Fact]
    public void ValidationResult_NotRetained_AfterValidation()
    {
        var weakRef = CreateAndReleaseValidationResult();

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        Assert.False(weakRef.IsAlive,
            "OutputUnit should be collectible after caller releases reference");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndReleaseValidationResult()
    {
        var result = JsonSchemaValidator.Validate(
            """{"type": "object", "properties": {"a": {"type": "string"}}, "required": ["a"]}""",
            """{"a": 123}""");

        // Result should have errors (type mismatch on "a")
        Assert.False(result.Valid);
        return new WeakReference(result);
    }

    [Fact]
    public void ParsedSchema_NotRetained_WhenReleased()
    {
        var weakRef = CreateAndReleaseParsedSchema();

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);

        Assert.False(weakRef.IsAlive,
            "IJsonSchema should be collectible after caller releases reference");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndReleaseParsedSchema()
    {
        var schema = JsonSchemaValidator.Parse("""{"type": "number", "maximum": 100}""");

        // Actually use it to ensure full initialization
        Assert.True(schema.IsValid("42"));
        return new WeakReference(schema);
    }

    #endregion
}
