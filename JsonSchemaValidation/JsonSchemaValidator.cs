// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.DependencyInjection;
using FormFinch.JsonSchemaValidation.Validation.Output;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation
{
    /// <summary>
    /// Static entry point for JSON Schema validation.
    /// Provides simple, one-liner validation without requiring dependency injection setup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides two usage patterns:
    /// </para>
    /// <para>
    /// <b>One-shot validation</b> - for validating a single instance:
    /// <code>
    /// var result = JsonSchemaValidator.Validate(schemaJson, instanceJson);
    /// </code>
    /// </para>
    /// <para>
    /// <b>Parsed schema</b> - for validating multiple instances against the same schema:
    /// <code>
    /// var schema = JsonSchemaValidator.Parse(schemaJson);
    /// var result1 = schema.Validate(instance1);
    /// var result2 = schema.Validate(instance2);
    /// </code>
    /// </para>
    /// <para>
    /// For advanced scenarios (custom configuration, dependency injection integration),
    /// use <see cref="SchemaValidationSetup.AddJsonSchemaValidation(IServiceCollection, Action{SchemaValidationOptions}?)"/> instead.
    /// </para>
    /// <para>
    /// <b>Note:</b> Schemas are cached by content hash to avoid unbounded memory growth
    /// when using one-shot validation repeatedly with the same schema.
    /// </para>
    /// <para>
    /// <b>Thread safety:</b> This static API is safe for concurrent use. All shared caches
    /// use thread-safe primitives. Custom <see cref="SchemaValidationOptions"/> should be
    /// treated as immutable after passing them to this API.
    /// </para>
    /// </remarks>
    public static class JsonSchemaValidator
    {
        private static readonly Lazy<ValidatorServices> DefaultServices = new(
            static () => CreateDefaultServices(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        // Cache schema content hash -> schema URI to avoid re-registering
        // when the same schema is validated multiple times via one-shot Validate().
        // Uses LRU eviction to preserve frequently-used schemas while bounding memory.
        private const int MaxCacheSize = 1000;
        private static readonly LruCache<string, Uri> SchemaCache = new(MaxCacheSize, StringComparer.Ordinal);

        #region Validate Methods

        /// <summary>
        /// Validates a JSON instance against a JSON Schema.
        /// </summary>
        /// <param name="schemaJson">The JSON Schema as a string.</param>
        /// <param name="instanceJson">The JSON instance to validate as a string.</param>
        /// <param name="format">The output format. Defaults to <see cref="OutputFormat.Basic"/>.</param>
        /// <returns>The validation result in the specified output format.</returns>
        /// <exception cref="JsonException">Thrown when the schema or instance JSON is invalid.</exception>
        /// <example>
        /// <code>
        /// var result = JsonSchemaValidator.Validate(
        ///     """{"type": "string", "minLength": 1}""",
        ///     "\"hello\""
        /// );
        /// if (result.Valid)
        ///     Console.WriteLine("Valid!");
        /// </code>
        /// </example>
        public static OutputUnit Validate(string schemaJson, string instanceJson, OutputFormat format = OutputFormat.Basic)
        {
            using var schemaDoc = JsonDocument.Parse(schemaJson);
            using var instanceDoc = JsonDocument.Parse(instanceJson);
            return Validate(schemaDoc.RootElement, instanceDoc.RootElement, format);
        }

        /// <summary>
        /// Validates a JSON instance against a JSON Schema.
        /// </summary>
        /// <param name="schema">The JSON Schema as a <see cref="JsonElement"/>.</param>
        /// <param name="instance">The JSON instance to validate.</param>
        /// <param name="format">The output format. Defaults to <see cref="OutputFormat.Basic"/>.</param>
        /// <returns>The validation result in the specified output format.</returns>
        /// <example>
        /// <code>
        /// var schemaElement = JsonDocument.Parse("""{"type": "integer"}""").RootElement;
        /// var instanceElement = JsonDocument.Parse("42").RootElement;
        /// var result = JsonSchemaValidator.Validate(schemaElement, instanceElement);
        /// </code>
        /// </example>
        public static OutputUnit Validate(JsonElement schema, JsonElement instance, OutputFormat format = OutputFormat.Basic)
        {
            var services = DefaultServices.Value;
            // Clone to ensure the JsonElement outlives the caller's JsonDocument
            return ValidateCore(services, schema.Clone(), instance, format);
        }

        /// <summary>
        /// Validates a JSON instance against a JSON Schema with custom options.
        /// </summary>
        /// <param name="schemaJson">The JSON Schema as a string.</param>
        /// <param name="instanceJson">The JSON instance to validate as a string.</param>
        /// <param name="options">Options to configure validation behavior.</param>
        /// <param name="format">The output format. Defaults to <see cref="OutputFormat.Basic"/>.</param>
        /// <returns>The validation result in the specified output format.</returns>
        public static OutputUnit Validate(string schemaJson, string instanceJson, SchemaValidationOptions options, OutputFormat format = OutputFormat.Basic)
        {
            using var schemaDoc = JsonDocument.Parse(schemaJson);
            using var instanceDoc = JsonDocument.Parse(instanceJson);
            return Validate(schemaDoc.RootElement, instanceDoc.RootElement, options, format);
        }

        /// <summary>
        /// Validates a JSON instance against a JSON Schema with custom options.
        /// </summary>
        /// <param name="schema">The JSON Schema as a <see cref="JsonElement"/>.</param>
        /// <param name="instance">The JSON instance to validate.</param>
        /// <param name="options">Options to configure validation behavior.</param>
        /// <param name="format">The output format. Defaults to <see cref="OutputFormat.Basic"/>.</param>
        /// <returns>The validation result in the specified output format.</returns>
        public static OutputUnit Validate(JsonElement schema, JsonElement instance, SchemaValidationOptions options, OutputFormat format = OutputFormat.Basic)
        {
            var services = CreateServicesFromOptions(options);
            // Clone to ensure the JsonElement outlives the caller's JsonDocument
            return ValidateCore(services, schema.Clone(), instance, format);
        }

        #endregion

        #region IsValid Methods

        /// <summary>
        /// Checks if a JSON instance is valid against a JSON Schema.
        /// </summary>
        /// <param name="schemaJson">The JSON Schema as a string.</param>
        /// <param name="instanceJson">The JSON instance to validate as a string.</param>
        /// <returns><c>true</c> if the instance is valid; otherwise, <c>false</c>.</returns>
        /// <exception cref="JsonException">Thrown when the schema or instance JSON is invalid.</exception>
        /// <example>
        /// <code>
        /// if (JsonSchemaValidator.IsValid("""{"type": "number"}""", "42"))
        ///     Console.WriteLine("Valid number!");
        /// </code>
        /// </example>
        public static bool IsValid(string schemaJson, string instanceJson)
        {
            using var schemaDoc = JsonDocument.Parse(schemaJson);
            using var instanceDoc = JsonDocument.Parse(instanceJson);
            return IsValid(schemaDoc.RootElement, instanceDoc.RootElement);
        }

        /// <summary>
        /// Checks if a JSON instance is valid against a JSON Schema.
        /// </summary>
        /// <param name="schema">The JSON Schema as a <see cref="JsonElement"/>.</param>
        /// <param name="instance">The JSON instance to validate.</param>
        /// <returns><c>true</c> if the instance is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValid(JsonElement schema, JsonElement instance)
        {
            var services = DefaultServices.Value;
            // Clone to ensure the JsonElement outlives the caller's JsonDocument
            return IsValidCore(services, schema.Clone(), instance);
        }

        #endregion

        #region Parse Methods

        /// <summary>
        /// Parses a JSON Schema for repeated validation.
        /// Use this when validating multiple instances against the same schema for better performance.
        /// </summary>
        /// <param name="schemaJson">The JSON Schema as a string.</param>
        /// <returns>A parsed schema that can validate multiple instances.</returns>
        /// <exception cref="JsonException">Thrown when the schema JSON is invalid.</exception>
        /// <example>
        /// <code>
        /// var schema = JsonSchemaValidator.Parse("""{"type": "string"}""");
        /// var result1 = schema.Validate("\"hello\"");
        /// var result2 = schema.Validate("\"world\"");
        /// </code>
        /// </example>
        public static IJsonSchema Parse(string schemaJson)
        {
            using var schemaDoc = JsonDocument.Parse(schemaJson);
            return Parse(schemaDoc.RootElement);
        }

        /// <summary>
        /// Parses a JSON Schema for repeated validation.
        /// Use this when validating multiple instances against the same schema for better performance.
        /// </summary>
        /// <param name="schema">The JSON Schema as a <see cref="JsonElement"/>.</param>
        /// <returns>A parsed schema that can validate multiple instances.</returns>
        public static IJsonSchema Parse(JsonElement schema)
        {
            var services = DefaultServices.Value;
            // Clone to ensure the JsonElement outlives the caller's JsonDocument
            return ParseCore(services, schema.Clone());
        }

        /// <summary>
        /// Parses a JSON Schema with custom options for repeated validation.
        /// </summary>
        /// <param name="schemaJson">The JSON Schema as a string.</param>
        /// <param name="options">Options to configure validation behavior.</param>
        /// <returns>A parsed schema that can validate multiple instances.</returns>
        public static IJsonSchema Parse(string schemaJson, SchemaValidationOptions options)
        {
            using var schemaDoc = JsonDocument.Parse(schemaJson);
            return Parse(schemaDoc.RootElement, options);
        }

        /// <summary>
        /// Parses a JSON Schema with custom options for repeated validation.
        /// </summary>
        /// <param name="schema">The JSON Schema as a <see cref="JsonElement"/>.</param>
        /// <param name="options">Options to configure validation behavior.</param>
        /// <returns>A parsed schema that can validate multiple instances.</returns>
        public static IJsonSchema Parse(JsonElement schema, SchemaValidationOptions options)
        {
            var services = CreateServicesFromOptions(options);
            // Clone to ensure the JsonElement outlives the caller's JsonDocument
            return ParseCore(services, schema.Clone());
        }

        #endregion

        #region Private Implementation

        private static ValidatorServices CreateDefaultServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddJsonSchemaValidation();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.InitializeSingletonServices();

            return new ValidatorServices(
                serviceProvider.GetRequiredService<ISchemaRepository>(),
                serviceProvider.GetRequiredService<ISchemaValidatorFactory>(),
                serviceProvider.GetRequiredService<IJsonValidationContextFactory>(),
                serviceProvider,
                isDefault: true);
        }

        private static ValidatorServices CreateServicesFromOptions(SchemaValidationOptions options)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddJsonSchemaValidation(options);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.InitializeSingletonServices();

            return new ValidatorServices(
                serviceProvider.GetRequiredService<ISchemaRepository>(),
                serviceProvider.GetRequiredService<ISchemaValidatorFactory>(),
                serviceProvider.GetRequiredService<IJsonValidationContextFactory>(),
                serviceProvider);
        }

        private static OutputUnit ValidateCore(ValidatorServices services, JsonElement schema, JsonElement instance, OutputFormat format)
        {
            var schemaUri = GetOrRegisterSchema(services, schema, useCache: services.IsDefault);
            if (schemaUri == null)
            {
                return new OutputUnit
                {
                    Valid = false,
                    InstanceLocation = "",
                    KeywordLocation = "",
                    Error = "Failed to register schema."
                };
            }

            var validator = services.SchemaValidatorFactory.GetValidator(schemaUri);
            var context = services.ContextFactory.CreateContextForRoot(instance);
            return validator.ValidateWithOutput(context, format);
        }

        private static bool IsValidCore(ValidatorServices services, JsonElement schema, JsonElement instance)
        {
            var schemaUri = GetOrRegisterSchema(services, schema, useCache: services.IsDefault);
            if (schemaUri == null)
            {
                return false;
            }

            var validator = services.SchemaValidatorFactory.GetValidator(schemaUri);
            var context = services.ContextFactory.CreateContextForRoot(instance);
            return validator.IsValid(context);
        }

        private static IJsonSchema ParseCore(ValidatorServices services, JsonElement schema)
        {
            var schemaUri = GetOrRegisterSchema(services, schema, useCache: services.IsDefault);
            if (schemaUri == null)
            {
                throw new InvalidOperationException("Failed to register schema.");
            }

            var validator = services.SchemaValidatorFactory.GetValidator(schemaUri);
            return new CompiledJsonSchema(validator, services.ContextFactory, schemaUri);
        }

        /// <summary>
        /// Gets a cached schema URI or registers the schema and caches it.
        /// Uses content-based hashing to identify duplicate schemas.
        /// </summary>
        private static Uri? GetOrRegisterSchema(ValidatorServices services, JsonElement schema, bool useCache)
        {
            string? hash = null;

            // Only use cache for default services (custom options create new service providers)
            if (useCache)
            {
                // Compute content hash for cache lookup
                hash = SchemaHasher.ComputeHash(schema);

                // Check cache first
                if (SchemaCache.TryGetValue(hash, out var cachedUri))
                {
                    return cachedUri;
                }
            }

            // Not in cache (or cache disabled), register the schema
            if (!services.SchemaRepository.TryRegisterSchema(schema, out var schemaData))
            {
                return null;
            }

            var schemaUri = schemaData!.SchemaUri!;

            // Cache the URI for future lookups (only for default services)
            if (useCache && hash != null)
            {
                SchemaCache.Set(hash, schemaUri);
            }

            return schemaUri;
        }

        private sealed class ValidatorServices
        {
            public ISchemaRepository SchemaRepository { get; }
            public ISchemaValidatorFactory SchemaValidatorFactory { get; }
            public IJsonValidationContextFactory ContextFactory { get; }
            public IServiceProvider ServiceProvider { get; }
            public bool IsDefault { get; }

            public ValidatorServices(
                ISchemaRepository schemaRepository,
                ISchemaValidatorFactory schemaValidatorFactory,
                IJsonValidationContextFactory contextFactory,
                IServiceProvider serviceProvider,
                bool isDefault = false)
            {
                SchemaRepository = schemaRepository;
                SchemaValidatorFactory = schemaValidatorFactory;
                ContextFactory = contextFactory;
                ServiceProvider = serviceProvider;
                IsDefault = isDefault;
            }
        }

        #endregion
    }
}
