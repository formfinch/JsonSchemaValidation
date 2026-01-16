using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.CompiledValidators;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidation.DependencyInjection
{
    public static class ServiceProviderExtensions
    {
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

            // Initialize registry-aware validators (must happen after all validators are registered)
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
