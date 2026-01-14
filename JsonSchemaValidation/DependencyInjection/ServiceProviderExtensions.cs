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

            // Register compiled validators if any were added
            var compiledValidators = serviceProvider.GetService<ICompiledValidator[]>();
            if (compiledValidators != null)
            {
                var registry = serviceProvider.GetRequiredService<ICompiledValidatorRegistry>();
                for (int i = 0; i < compiledValidators.Length; i++)
                {
                    registry.Register(compiledValidators[i]);
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
