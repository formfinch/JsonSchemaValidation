using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Draft202012;
using JsonSchemaValidation.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            services.AddSingleton<ISchemaValidatorFactory, SchemaValidatorFactory>();

            if (options.EnableDraft202012)
            {
                services.AddDraft202012();
            }

            return services;
        }
    }
}
