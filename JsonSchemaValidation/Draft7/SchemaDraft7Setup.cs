using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft7.Keywords;
using Microsoft.Extensions.DependencyInjection;

namespace JsonSchemaValidation.Draft7
{
    public static class SchemaDraft7Setup
    {
        /// <summary>
        /// The draft version key used for keyed service registration.
        /// Note: Draft 7 URIs historically included a trailing '#' fragment.
        /// We use the non-fragment version internally for consistency with the repository.
        /// </summary>
        public const string DraftVersion = "http://json-schema.org/draft-07/schema";

        public static IServiceCollection AddDraft7(this IServiceCollection services)
        {
            services.AddSingleton<ISchemaDraftMeta, SchemaDraft7Meta>();
            services.AddSingleton<ISchemaDraftValidatorFactory, SchemaDraft7ValidatorFactory>();

            // Register keyword validators with keyed services using draft version as key
            // Boolean schema validators
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, BooleanValidatorFactory>(DraftVersion);

            // Core keywords
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, RefValidatorFactory>(DraftVersion);

            // Validation keywords
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, TypeValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, TypeMultipleTypesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ConstValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, EnumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MultipleOfValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaximumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinimumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ExclusiveMaximumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ExclusiveMinimumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaxLengthValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinLengthValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PatternValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaxItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, UniqueItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaxPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, RequiredValidatorFactory>(DraftVersion);

            // Applicator keywords
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AllOfValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AnyOfValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, OneOfValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, NotValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, IfThenElseValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PatternPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AdditionalPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PropertyNamesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, DependenciesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AdditionalItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ContainsValidatorFactory>(DraftVersion);

            // Format keyword
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, FormatValidatorFactory>(DraftVersion);

            // Content keywords
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ContentEncodingValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ContentMediaTypeValidatorFactory>(DraftVersion);

            return services;
        }
    }
}
