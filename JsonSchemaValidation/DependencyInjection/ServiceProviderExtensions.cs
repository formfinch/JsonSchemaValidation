using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // load draft meta schemas
            var schemaRepository = serviceProvider.GetRequiredService<ISchemaRepository>();
            foreach (var draft in serviceProvider.GetServices<ISchemaDraftMeta>())
            {
                foreach (var schema in draft.Schemas)
                {
                    _ = schemaRepository.AddSchema(new SchemaMetadata(schema));
                }
            }
        }
    }
}
