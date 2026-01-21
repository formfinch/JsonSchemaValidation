// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Repositories;
using FormFinch.JsonSchemaValidation.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidationTests.Draft202012.ThreadSafety;

/// <summary>
/// Thread safety regression tests to verify the library handles concurrent access correctly.
/// Tests cover concurrent schema registration, validation, and service initialization.
/// </summary>
[Trait("Draft", "2020-12")]
public class RegressionTests
{
    #region Concurrent Validation Tests

    [Fact]
    public async Task ConcurrentValidation_SameValidator_AllSucceed()
    {
        // Arrange
        var (validator, contextFactory) = CreateValidatorForSchema("""{"type": "string"}""");
        var instance = JsonDocument.Parse("\"hello\"").RootElement;

        // Act - 100 parallel validations with the same validator
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var context = contextFactory.CreateContextForRoot(instance);
            return validator.ValidateRoot(context);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed without exceptions
        Assert.Equal(100, results.Length);
        Assert.All(results, r => Assert.True(r.IsValid));
    }

    [Fact]
    public async Task ConcurrentValidation_DifferentInstances_AllSucceed()
    {
        // Arrange
        var (validator, contextFactory) = CreateValidatorForSchema("""
            {
                "type": "object",
                "properties": {
                    "name": {"type": "string"},
                    "age": {"type": "integer", "minimum": 0}
                }
            }
            """);

        // Act - 100 parallel validations with different instances
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            var instance = JsonDocument.Parse($$$"""{"name": "User{{{i}}}", "age": {{{i}}}}""").RootElement;
            var context = contextFactory.CreateContextForRoot(instance);
            return validator.ValidateRoot(context);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, results.Length);
        Assert.All(results, r => Assert.True(r.IsValid));
    }

    [Fact]
    public async Task ConcurrentValidation_MixedValidAndInvalid_CorrectResults()
    {
        // Arrange
        var (validator, contextFactory) = CreateValidatorForSchema("""{"type": "number", "minimum": 50}""");

        // Act - 100 validations, half valid (>= 50), half invalid (< 50)
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            var instance = JsonDocument.Parse(i.ToString()).RootElement;
            var context = contextFactory.CreateContextForRoot(instance);
            var result = validator.ValidateRoot(context);
            return (Index: i, Result: result);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, results.Length);
        foreach (var (index, result) in results)
        {
            if (index >= 50)
                Assert.True(result.IsValid, $"Index {index} should be valid");
            else
                Assert.False(result.IsValid, $"Index {index} should be invalid");
        }
    }

    [Fact]
    public async Task ConcurrentValidation_ComplexSchema_NoRaceConditions()
    {
        // Arrange - Complex schema with allOf, anyOf, $ref patterns
        var (validator, contextFactory) = CreateValidatorForSchema("""
            {
                "allOf": [
                    {"type": "object"},
                    {
                        "anyOf": [
                            {"required": ["name"]},
                            {"required": ["id"]}
                        ]
                    }
                ],
                "properties": {
                    "name": {"type": "string"},
                    "id": {"type": "integer"}
                }
            }
            """);

        var validInstance = JsonDocument.Parse("""{"name": "test"}""").RootElement;

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var context = contextFactory.CreateContextForRoot(validInstance);
            return validator.ValidateRoot(context);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.True(r.IsValid));
    }

    #endregion

    #region Concurrent Schema Registration Tests

    [Fact]
    public async Task ConcurrentSchemaRegistration_DifferentSchemas_AllSucceed()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        // Act - Register 50 different schemas concurrently
        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            var schema = JsonDocument.Parse($$$"""{"type": "object", "title": "Schema{{{i}}}"}""").RootElement;
            return schemaRepository.TryRegisterSchema(schema, out _);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert - All registrations should succeed
        Assert.All(results, r => Assert.True(r));
    }

    [Fact]
    public async Task ConcurrentRegistrationAndValidation_NoDeadlocks()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var validatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        var contextFactory = serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        // Register an initial schema
        var initialSchema = JsonDocument.Parse("""{"type": "string"}""").RootElement;
        schemaRepository.TryRegisterSchema(initialSchema, out var schemaData);
        var validator = validatorFactory.GetValidator(schemaData!.SchemaUri!);

        // Act - Mix of registrations and validations in parallel
        var validationTasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            var instance = JsonDocument.Parse("\"test\"").RootElement;
            var context = contextFactory.CreateContextForRoot(instance);
            return validator.ValidateRoot(context);
        }));

        var registrationTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            var schema = JsonDocument.Parse($$$"""{"type": "number", "minimum": {{{i}}}}""").RootElement;
            return schemaRepository.TryRegisterSchema(schema, out _);
        }));

        // Wait for all tasks with a timeout to detect deadlocks
        var allValidationTasks = Task.WhenAll(validationTasks);
        var allRegistrationTasks = Task.WhenAll(registrationTasks);
        var combinedTask = Task.WhenAll(allValidationTasks, allRegistrationTasks);

        var completedTask = await Task.WhenAny(
            combinedTask,
            Task.Delay(TimeSpan.FromSeconds(10))
        );

        // Assert - Should complete without timeout (deadlock)
        Assert.Equal(combinedTask, completedTask);
    }

    #endregion

    #region Concurrent Initialization Tests

    [Fact]
    public async Task ConcurrentServiceProviderCreation_NoExceptions()
    {
        // Act - Create 50 service providers in parallel
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            var sp = CreateServiceProvider();
            // Should not throw
            return sp.GetRequiredService<ISchemaRepository>();
        }));

        var repositories = await Task.WhenAll(tasks);

        // Assert - All should succeed
        Assert.Equal(50, repositories.Length);
        Assert.All(repositories, r => Assert.NotNull(r));
    }

    [Fact]
    public async Task ConcurrentInitializeSingletonServices_NoExceptions()
    {
        // Act - Initialize services in parallel (tests SchemaDraft202012Meta thread safety)
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            var services = new ServiceCollection();
            services.AddJsonSchemaValidation(opt => opt.EnableDraft202012 = true);
            var sp = services.BuildServiceProvider();
            sp.InitializeSingletonServices();
            return sp;
        }));

        var providers = await Task.WhenAll(tasks);

        // Assert - All should succeed without race condition in meta schema loading
        Assert.Equal(50, providers.Length);
    }

    [Fact]
    public async Task ConcurrentMetaSchemaAccess_ConsistentResults()
    {
        // Arrange - Initialize once
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        // Act - Concurrently access the draft meta-schema
        var metaSchemaUri = new Uri("https://json-schema.org/draft/2020-12/schema");
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            return schemaRepository.GetSchema(metaSchemaUri);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert - All should return the same schema
        Assert.All(results, r => Assert.NotNull(r));
        var firstDraftVersion = results[0].DraftVersion;
        Assert.All(results, r => Assert.Equal(firstDraftVersion, r.DraftVersion));
    }

    #endregion

    #region Stress Tests

    [Fact]
    public async Task HighConcurrency_StressTest_NoFailures()
    {
        // Arrange
        var (validator, contextFactory) = CreateValidatorForSchema("""
            {
                "type": "array",
                "items": {"type": "integer"},
                "minItems": 1,
                "maxItems": 10
            }
            """);

        // Act - 500 parallel validations
        var random = new Random(42); // Fixed seed for reproducibility
        var tasks = Enumerable.Range(0, 500).Select(_ => Task.Run(() =>
        {
            var arrayLength = random.Next(1, 15);
            var items = string.Join(", ", Enumerable.Range(0, arrayLength).Select(i => i.ToString()));
            var instance = JsonDocument.Parse($"[{items}]").RootElement;
            var context = contextFactory.CreateContextForRoot(instance);
            return validator.ValidateRoot(context);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete without exceptions
        Assert.Equal(500, results.Length);
    }

    [Fact]
    public async Task RapidSchemaRegistration_SortedSchemasRemainConsistent()
    {
        // Arrange - This specifically tests the _sortedSchemas volatile field
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();

        // Register schemas rapidly while querying
        var registrationTasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            var schema = JsonDocument.Parse($$$"""
                {
                    "$id": "http://example.com/schema{{{i}}}",
                    "$dynamicAnchor": "anchor{{{i}}}",
                    "type": "object"
                }
                """).RootElement;
            schemaRepository.TryRegisterSchema(schema, out SchemaMetadata? _);
        }));

        var queryTasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            // This exercises TryGetDynamicRef which reads _sortedSchemas
            schemaRepository.TryGetDynamicRef("#anchor0", out SchemaMetadata? _);
        }));

        // Act - Run both in parallel
        await Task.WhenAll(registrationTasks.Concat(queryTasks));

        // Assert - Should complete without exceptions (InvalidOperationException during enumeration)
        // If we get here without exception, the test passes
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

    private static (ISchemaValidator Validator, IJsonValidationContextFactory ContextFactory) CreateValidatorForSchema(string schemaJson)
    {
        var serviceProvider = CreateServiceProvider();
        var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var validatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        var contextFactory = serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        var schema = JsonDocument.Parse(schemaJson).RootElement;
        if (!schemaRepository.TryRegisterSchema(schema, out var schemaData))
        {
            throw new InvalidOperationException("Failed to register schema.");
        }

        var validator = validatorFactory.GetValidator(schemaData!.SchemaUri!);
        return (validator, contextFactory);
    }

    #endregion
}
