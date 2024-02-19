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
            services.AddSingleton<ISchemaDraftMeta, SchemaDraft202012Meta>();
            services.AddSingleton<ISchemaDraftValidatorFactory, SchemaDraft202012ValidatorFactory>();

            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, AdditionalPropertiesValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, AllOfValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, AnyOfValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, BooleanValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, ContainsValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, ConstValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, DependentRequiredValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, DependentSchemasValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, EnumValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, ExclusiveMaximumValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, ExclusiveMinimumValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, FormatValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, IfThenElseValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, ItemsValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MaximumValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MaxItemsValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MaxLengthValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MaxPropertiesValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MinimumValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MinItemsValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MinLengthValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MinPropertiesValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, MultipleOfValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, NotValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, OneOfValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, PatternValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, PatternPropertiesValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, PrefixItemsValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, PropertiesValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, RequiredValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, TypeValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, TypeMultipleTypesValidatorFactory>();
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, UniqueItemsValidatorFactory>();

            // todo: validation relies on these validators being run last, supply a guaranteed validator order system
            services.AddSingleton<ISchemaDraftKeywordValidatorFactory, UnevaluatedItemsValidatorFactory>();

            return services;
        }
    }
}
