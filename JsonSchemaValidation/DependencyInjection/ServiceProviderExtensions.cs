// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.CompiledValidators;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.DependencyInjection
{
    /// <summary>
    /// Extension methods for initializing JSON Schema validation services after the service provider is built.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Initializes all singleton validation services, registers compiled metaschema validators,
        /// and loads draft meta schemas into the schema repository.
        /// Must be called after building the service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider to initialize services from.</param>
        /// <exception cref="InvalidOperationException">Thrown when a draft meta schema cannot be registered.</exception>
        public static void InitializeSingletonServices(this IServiceProvider serviceProvider)
        {
            // List the types of the singleton services you want to initialize
            var singletonServices = new List<Type>
            {
                typeof(ResolveLazyInterfaces),
                // ... add other singleton services here if needed
            };

            foreach (var serviceType in singletonServices)
            {
                // This will force the instantiation of the singleton service
                serviceProvider.GetRequiredService(serviceType);
            }

            // Register compiled metaschema validators (always loaded by default)
            var registry = serviceProvider.GetRequiredService<ICompiledValidatorRegistry>();
            var metaschemaValidators = CompiledMetaschemas.GetAll();
            for (int i = 0; i < metaschemaValidators.Length; i++)
            {
                registry.Register(metaschemaValidators[i]);
            }

            // Register any additional compiled validators added via AddCompiledValidators
            var compiledValidators = serviceProvider.GetService<ICompiledValidator[]>();
            if (compiledValidators != null)
            {
                for (int i = 0; i < compiledValidators.Length; i++)
                {
                    registry.Register(compiledValidators[i]);
                }
            }

            // Two-phase initialization of registry-aware validators:
            // Phase 1: Register all subschemas first (so they're available for external refs)
            foreach (var validator in metaschemaValidators)
            {
                if (validator is IRegistryAwareCompiledValidator registryAware)
                {
                    registryAware.RegisterSubschemas(registry);
                }
            }

            if (compiledValidators != null)
            {
                foreach (var validator in compiledValidators)
                {
                    if (validator is IRegistryAwareCompiledValidator registryAware)
                    {
                        registryAware.RegisterSubschemas(registry);
                    }
                }
            }

            // Phase 2: Initialize (resolve external refs) after all subschemas are registered
            foreach (var validator in metaschemaValidators)
            {
                if (validator is IRegistryAwareCompiledValidator registryAware)
                {
                    registryAware.Initialize(registry);
                }
            }

            if (compiledValidators != null)
            {
                foreach (var validator in compiledValidators)
                {
                    if (validator is IRegistryAwareCompiledValidator registryAware)
                    {
                        registryAware.Initialize(registry);
                    }
                }
            }

            // load draft meta schemas
            var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
            var drafts = serviceProvider.GetServices<ISchemaDraftMeta>();
            for (int i = 0; drafts.Skip(i).Any(); i++)
            {
                var draft = drafts.ElementAt(i);
                var schemas = draft.Schemas;
                for (int j = 0; schemas.Skip(j).Any(); j++)
                {
                    var schema = schemas.ElementAt(j);
                    if (!schemaRepository.TryRegisterSchema(schema, out _))
                    {
                        throw new InvalidOperationException("Schema could not be registered.");
                    }
                }
            }
        }
    }
}
