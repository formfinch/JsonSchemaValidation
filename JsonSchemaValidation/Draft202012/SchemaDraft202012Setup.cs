using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Draft202012.Keywords;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012
{
    public static class SchemaDraft202012Setup
    {
        public static IServiceCollection AddDraft202012(this IServiceCollection services)
        {
            services.AddSingleton<ISchemaDraftFactory, SchemaDraft202012Factory>();

            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, ConstValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, ExclusiveMaximumValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, ExclusiveMinimumValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MaximumValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MaxItemsValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MaxLengthValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MinimumValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MinItemsValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MinLengthValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MultipleOfValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, UniqueItemsValidatorFactory>();

            return services;
        }
    }
}
