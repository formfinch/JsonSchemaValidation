// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Tests.Stress.Concurrency;

/// <summary>
/// Stress tests for validating thread-safety claims under heavy load.
/// Tests cover LruCache, SchemaRepository, Static API, and CompiledValidatorRegistry.
/// </summary>
/// <remarks>
/// These tests are in a separate project and not run by default.
/// Run with: dotnet test JsonSchemaValidationTests.Stress
/// </remarks>
[Trait("Category", "Stress")]
public class StressTests
{
    private const int HighConcurrency = 100;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

    #region LruCache Stress Tests

    [Fact]
    public async Task LruCache_ConcurrentSetAndGet_NoDataCorruption()
    {
        var cache = new LruCache<int, string>(100);

        // Run 100 threads doing concurrent Set/TryGetValue
        var tasks = Enumerable.Range(0, HighConcurrency).Select(threadId => Task.Run(() =>
        {
            var random = new Random(threadId);
            for (int i = 0; i < 1000; i++)
            {
                var key = random.Next(0, 200); // Keys overlap to create contention
                var value = $"thread{threadId}-{i}";

                // Mix of reads and writes
                if (random.Next(2) == 0)
                {
                    cache.Set(key, value);
                }
                else
                {
                    cache.TryGetValue(key, out _);
                }
            }
        }));

        var allTasks = Task.WhenAll(tasks);
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(TestTimeout));

        Assert.Equal(allTasks, completedTask); // Should complete before timeout
        Assert.True(cache.Count <= 100); // Should respect capacity
    }

    [Fact]
    public async Task LruCache_HighVolumeOperations_MaintainsConsistency()
    {
        var cache = new LruCache<string, int>(50);
        var operationCount = 0;

        var tasks = Enumerable.Range(0, HighConcurrency).Select(threadId => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var key = $"key-{threadId % 100}";
                cache.Set(key, threadId);
                cache.TryGetValue(key, out _);
                Interlocked.Increment(ref operationCount);
            }
        }));

        await Task.WhenAll(tasks);

        Assert.Equal(HighConcurrency * 100, operationCount);
        Assert.True(cache.Count <= 50);
    }

    [Fact]
    public void LruCache_RapidEviction_CorrectBehavior()
    {
        // Cache with capacity 1 - every Set evicts
        var cache = new LruCache<int, int>(1);
        var evictionCount = 0;

        for (int i = 0; i < 1000; i++)
        {
            if (cache.Count == 1)
            {
                evictionCount++;
            }
            cache.Set(i, i);
        }

        Assert.Equal(1, cache.Count);
        // After first item, all subsequent Sets cause eviction
        Assert.Equal(999, evictionCount);
    }

    #endregion

    #region SchemaRepository Stress Tests

    [Fact]
    public async Task SchemaRepository_ConcurrentRegistration_AllSucceed()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var successCount = 0;

        // 50 threads registering different schemas simultaneously
        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            using var doc = JsonDocument.Parse($$$"""
                {
                    "$id": "http://example.com/concurrent-test-{{{i}}}",
                    "type": "object",
                    "title": "Schema{{{i}}}"
                }
                """);

            if (schemaRepository.TryRegisterSchema(doc.RootElement, out _))
            {
                Interlocked.Increment(ref successCount);
            }
        }));

        var allTasks = Task.WhenAll(tasks);
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(TestTimeout));

        Assert.Equal(allTasks, completedTask);
        Assert.Equal(50, successCount);
    }

    [Fact]
    public async Task SchemaRepository_SameSchemaRegistration_Idempotent()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var successCount = 0;

        // 50 threads registering the SAME schema (idempotence test)
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            using var doc = JsonDocument.Parse("""
                {
                    "$id": "http://example.com/idempotent-test",
                    "type": "string"
                }
                """);

            if (schemaRepository.TryRegisterSchema(doc.RootElement, out SchemaMetadata? _))
            {
                Interlocked.Increment(ref successCount);
            }
        }));

        var allTasks = Task.WhenAll(tasks);
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(TestTimeout));

        Assert.Equal(allTasks, completedTask);
        // All should "succeed" (idempotent - first one registers, rest find it already registered)
        // The implementation returns true for first registration only
        Assert.True(successCount >= 1);
    }

    [Fact]
    public async Task SchemaRepository_MixedReadWrite_NoDeadlocks()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var validatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        var contextFactory = serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        // Pre-register a schema
        using var initialDoc = JsonDocument.Parse("""
            {
                "$id": "http://example.com/mixed-test",
                "type": "object"
            }
            """);
        schemaRepository.TryRegisterSchema(initialDoc.RootElement, out var schemaData);
        var validator = validatorFactory.GetValidator(schemaData!.SchemaUri!);

        // Run mixed registration and validation in parallel
        var registrationTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            using var doc = JsonDocument.Parse($$$"""{"type": "number", "minimum": {{{i}}}}""");
            schemaRepository.TryRegisterSchema(doc.RootElement, out _);
        }));

        var validationTasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            using var instanceDoc = JsonDocument.Parse("{}");
            var context = contextFactory.CreateContextForRoot(instanceDoc.RootElement);
            return validator.ValidateRoot(context);
        }));

        var allTasks = Task.WhenAll(registrationTasks.Concat(validationTasks));
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(TestTimeout));

        // Should complete without deadlock
        Assert.Equal(allTasks, completedTask);
    }

    [Fact]
    public async Task SchemaRepository_DynamicRefLookupDuringRegistration_NoRaceConditions()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        // Register schemas with dynamic anchors while querying
        var registrationTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            using var doc = JsonDocument.Parse($$$"""
                {
                    "$id": "http://example.com/dynamic-{{{i}}}",
                    "$dynamicAnchor": "anchor{{{i}}}",
                    "type": "object"
                }
                """);
            schemaRepository.TryRegisterSchema(doc.RootElement, out SchemaMetadata? _);
        }));

        var queryTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            // Query for dynamic anchors (some may not exist yet)
            schemaRepository.TryGetDynamicRef($"#anchor{i % 10}", out SchemaMetadata? _);
        }));

        var allTasks = Task.WhenAll(registrationTasks.Concat(queryTasks));
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(TestTimeout));

        Assert.Equal(allTasks, completedTask);
    }

    #endregion

    #region Static API Stress Tests

    [Fact]
    public async Task StaticApi_ConcurrentValidationSameSchema_UsesCaching()
    {
        var schema = """{"type": "string", "minLength": 1}""";
        var validationCount = 0;

        // 100 threads calling Validate with the same schema
        var tasks = Enumerable.Range(0, HighConcurrency).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var result = JsonSchemaValidator.Validate(schema, "\"hello\"");
                Assert.True(result.Valid);
                Interlocked.Increment(ref validationCount);
            }
        }));

        var allTasks = Task.WhenAll(tasks);
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(TestTimeout));

        Assert.Equal(allTasks, completedTask);
        Assert.Equal(HighConcurrency * 100, validationCount);
    }

    [Fact]
    public async Task StaticApi_ConcurrentValidationDifferentSchemas_AllSucceed()
    {
        var validationCount = 0;

        // 100 threads calling Validate with different schemas
        var tasks = Enumerable.Range(0, HighConcurrency).Select(threadId => Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                var fieldName = $"field{threadId}_{i}";
                var schema = "{\"type\": \"object\", \"properties\": {\"" + fieldName + "\": {\"type\": \"string\"}}}";
                var instance = "{\"" + fieldName + "\": \"value\"}";

                var result = JsonSchemaValidator.Validate(schema, instance);
                Assert.True(result.Valid);
                Interlocked.Increment(ref validationCount);
            }
        }));

        var allTasks = Task.WhenAll(tasks);
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(TestTimeout));

        Assert.Equal(allTasks, completedTask);
        Assert.Equal(HighConcurrency * 10, validationCount);
    }

    [Fact]
    public async Task StaticApi_ParsedSchemaConcurrentValidation_ThreadSafe()
    {
        var schema = JsonSchemaValidator.Parse("""
            {
                "type": "object",
                "properties": {
                    "name": {"type": "string"},
                    "age": {"type": "integer", "minimum": 0}
                },
                "required": ["name"]
            }
            """);

        var validCount = 0;
        var invalidCount = 0;

        // 100 threads using the same parsed schema
        var tasks = Enumerable.Range(0, HighConcurrency).Select(threadId => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var instance = (i % 2 == 0)
                    ? """{"name": "test", "age": 25}"""
                    : """{"age": -1}"""; // Invalid - missing required, negative age

                if (schema.IsValid(instance))
                {
                    Interlocked.Increment(ref validCount);
                }
                else
                {
                    Interlocked.Increment(ref invalidCount);
                }
            }
        }));

        var allTasks = Task.WhenAll(tasks);
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(TestTimeout));

        Assert.Equal(allTasks, completedTask);
        Assert.Equal(HighConcurrency * 50, validCount);
        Assert.Equal(HighConcurrency * 50, invalidCount);
    }

    [Fact]
    public async Task StaticApi_IsValid_HighThroughput()
    {
        var schema = """{"type": "integer", "minimum": 0, "maximum": 100}""";
        var trueCount = 0;
        var falseCount = 0;

        var tasks = Enumerable.Range(0, HighConcurrency).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                // Alternate between valid (50) and invalid (150)
                var value = (i % 2 == 0) ? "50" : "150";

                if (JsonSchemaValidator.IsValid(schema, value))
                {
                    Interlocked.Increment(ref trueCount);
                }
                else
                {
                    Interlocked.Increment(ref falseCount);
                }
            }
        }));

        await Task.WhenAll(tasks);

        Assert.Equal(HighConcurrency * 50, trueCount);
        Assert.Equal(HighConcurrency * 50, falseCount);
    }

    #endregion

    #region Validator Registry Stress Tests

    [Fact]
    public async Task ValidatorFactory_ConcurrentLookup_NoDeadlocks()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var validatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();

        // Pre-register multiple schemas
        var schemaUris = new List<Uri>();
        var schemaDocs = new List<JsonDocument>();
        for (int i = 0; i < 10; i++)
        {
            var doc = JsonDocument.Parse($$$"""
                {
                    "$id": "http://example.com/validator-lookup-{{{i}}}",
                    "type": "object",
                    "properties": {"value": {"type": "number"}}
                }
                """);
            schemaDocs.Add(doc);

            if (schemaRepository.TryRegisterSchema(doc.RootElement, out var data))
            {
                schemaUris.Add(data!.SchemaUri!);
            }
        }

        // Verify registrations succeeded before proceeding
        Assert.NotEmpty(schemaUris);

        // Concurrent validator lookups
        var tasks = Enumerable.Range(0, HighConcurrency).Select(_ => Task.Run(() =>
        {
            var random = new Random();
            for (int i = 0; i < 100; i++)
            {
                var uri = schemaUris[random.Next(schemaUris.Count)];
                var validator = validatorFactory.GetValidator(uri);
                Assert.NotNull(validator);
            }
        }));

        var allTasks = Task.WhenAll(tasks);
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.Equal(allTasks, completedTask); // Should complete without deadlock

        // Cleanup
        foreach (var doc in schemaDocs)
        {
            doc.Dispose();
        }
    }

    [Fact]
    public async Task ValidatorFactory_ConcurrentLookupDuringRegistration_NoExceptions()
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var validatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();

        // Pre-register one schema
        using var initialDoc = JsonDocument.Parse("""
            {
                "$id": "http://example.com/concurrent-lookup-base",
                "type": "string"
            }
            """);
        schemaRepository.TryRegisterSchema(initialDoc.RootElement, out var baseData);
        var baseUri = baseData!.SchemaUri!;

        // Concurrent registration and lookup
        var exceptionCount = 0;

        var registrationTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            using var doc = JsonDocument.Parse($$$"""
                {
                    "$id": "http://example.com/concurrent-lookup-{{{i}}}",
                    "type": "integer"
                }
                """);
            schemaRepository.TryRegisterSchema(doc.RootElement, out _);
        }));

        var lookupTasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            try
            {
                var validator = validatorFactory.GetValidator(baseUri);
                Assert.NotNull(validator);
            }
            catch
            {
                Interlocked.Increment(ref exceptionCount);
            }
        }));

        await Task.WhenAll(registrationTasks.Concat(lookupTasks));

        Assert.Equal(0, exceptionCount);
    }

    #endregion

    #region Complex Schema Stress Tests

    [Fact]
    public async Task ComplexSchema_ConcurrentValidation_CorrectResults()
    {
        var schema = """
            {
                "type": "object",
                "properties": {
                    "user": {
                        "type": "object",
                        "properties": {
                            "name": {"type": "string", "minLength": 1},
                            "email": {"type": "string", "format": "email"},
                            "age": {"type": "integer", "minimum": 0, "maximum": 150}
                        },
                        "required": ["name", "email"]
                    },
                    "items": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "id": {"type": "integer"},
                                "name": {"type": "string"}
                            },
                            "required": ["id"]
                        },
                        "minItems": 1
                    }
                },
                "required": ["user"]
            }
            """;

        var validInstance = """
            {
                "user": {"name": "John", "email": "john@example.com", "age": 30},
                "items": [{"id": 1, "name": "Item 1"}, {"id": 2}]
            }
            """;

        var invalidInstance = """
            {
                "user": {"name": "", "email": "invalid"},
                "items": []
            }
            """;

        var validCount = 0;
        var invalidCount = 0;

        var tasks = Enumerable.Range(0, HighConcurrency).Select(i => Task.Run(() =>
        {
            var instance = (i % 2 == 0) ? validInstance : invalidInstance;
            var result = JsonSchemaValidator.Validate(schema, instance);

            if (result.Valid)
            {
                Interlocked.Increment(ref validCount);
            }
            else
            {
                Interlocked.Increment(ref invalidCount);
            }
        }));

        await Task.WhenAll(tasks);

        Assert.Equal(HighConcurrency / 2, validCount);
        Assert.Equal(HighConcurrency / 2, invalidCount);
    }

    #endregion

    #region Helper Methods

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation(opt => opt.EnableDraft202012 = true);
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.InitializeSingletonServices();
        return serviceProvider;
    }

    #endregion
}
