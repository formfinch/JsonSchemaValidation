using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.CompiledValidators;
using JsonSchemaValidation.Draft3;
using JsonSchemaValidation.Draft4;
using JsonSchemaValidation.Draft6;
using JsonSchemaValidation.Draft7;
using JsonSchemaValidation.Draft201909;
using JsonSchemaValidation.Draft202012;
using JsonSchemaValidation.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidation.DependencyInjection
{
    public static class SchemaValidationSetup
    {
        public static IServiceCollection AddJsonSchemaValidation(this IServiceCollection services, Action<SchemaValidationOptions>? setupAction = null)
        {
            var options = new SchemaValidationOptions();
            setupAction?.Invoke(options);
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
    }
}
