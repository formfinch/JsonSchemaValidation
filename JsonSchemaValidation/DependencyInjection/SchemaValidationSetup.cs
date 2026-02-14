// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Common.Keywords;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using FormFinch.JsonSchemaValidation.Draft3;
using FormFinch.JsonSchemaValidation.Draft4;
using FormFinch.JsonSchemaValidation.Draft6;
using FormFinch.JsonSchemaValidation.Draft7;
using FormFinch.JsonSchemaValidation.Draft201909;
using FormFinch.JsonSchemaValidation.Draft202012;
using FormFinch.JsonSchemaValidation.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring JSON Schema validation services in a dependency injection container.
    /// </summary>
    public static class SchemaValidationSetup
    {
        /// <summary>
        /// Adds JSON Schema validation services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="setupAction">An optional action to configure <see cref="SchemaValidationOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddJsonSchemaValidation(options =>
        /// {
        ///     options.EnableDraft3 = false;
        ///     options.Draft202012.FormatAssertionEnabled = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddJsonSchemaValidation(this IServiceCollection services, Action<SchemaValidationOptions>? setupAction = null)
        {
            var options = new SchemaValidationOptions();
            setupAction?.Invoke(options);
            return services.AddJsonSchemaValidation(options);
        }

        /// <summary>
        /// Adds JSON Schema validation services to the specified <see cref="IServiceCollection"/> using the provided options.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="options">The options that control validation behavior and supported drafts.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddJsonSchemaValidation(this IServiceCollection services, SchemaValidationOptions options)
        {
            services.AddSingleton(options);

            services.AddSingleton<ISchemaRepository, SchemaRepository>();
            services.AddSingleton<ISchemaFactory, SchemaFactory>();
            services.AddSingleton<ICompiledValidatorRegistry, CompiledValidatorRegistry>();
            services.AddSingleton<ISchemaValidatorFactory, SchemaValidatorFactory>();
            services.AddSingleton<ILazySchemaValidatorFactory, LazySchemaValidatorFactory>();
            services.AddSingleton<IJsonValidationContextFactory, JsonValidationContextFactory>();
            services.AddSingleton<ResolveLazyInterfaces>();

            if (options.EnableDraft202012)
            {
                services.AddDraft202012();
            }

            if (options.EnableDraft201909)
            {
                services.AddDraft201909();
            }

            if (options.EnableDraft7)
            {
                services.AddDraft7();
            }

            if (options.EnableDraft6)
            {
                services.AddDraft6();
            }

            if (options.EnableDraft4)
            {
                services.AddDraft4();
            }

            if (options.EnableDraft3)
            {
                services.AddDraft3();
            }

            return services;
        }

        /// <summary>
        /// Registers compiled validators with the compiled validator registry.
        /// Call this after AddJsonSchemaValidation and before building the service provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="validators">The compiled validators to register.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCompiledValidators(
            this IServiceCollection services,
            IEnumerable<ICompiledValidator> validators)
        {
            // Store validators as array to be registered after the service provider is built
            services.AddSingleton(validators.ToArray());
            return services;
        }

        /// <summary>
        /// Registers compiled validators with the compiled validator registry.
        /// Call this after AddJsonSchemaValidation and before building the service provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="validators">The compiled validators to register.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCompiledValidators(
            this IServiceCollection services,
            params ICompiledValidator[] validators)
        {
            return services.AddCompiledValidators(validators.AsEnumerable());
        }

        /// <summary>
        /// Registers a custom annotation-only keyword for all draft versions.
        /// The keyword will always pass validation and emit the schema value as an annotation.
        /// Call this after <see cref="AddJsonSchemaValidation(IServiceCollection, Action{SchemaValidationOptions}?)"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="keyword">The keyword name (e.g., "x-display-order").</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAnnotationKeyword(
            this IServiceCollection services, string keyword)
        {
            var factory = new AnnotationKeywordValidatorFactory(keyword, ignoreVocabularyFilter: true);

            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(SchemaDraft202012Setup.DraftVersion, factory);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(SchemaDraft201909Setup.DraftVersion, factory);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(SchemaDraft7Setup.DraftVersion, factory);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(SchemaDraft6Setup.DraftVersion, factory);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(SchemaDraft4Setup.DraftVersion, factory);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(SchemaDraft3Setup.DraftVersion, factory);

            return services;
        }
    }
}
