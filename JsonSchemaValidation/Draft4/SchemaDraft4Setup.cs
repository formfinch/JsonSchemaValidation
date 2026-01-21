using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Draft4.Keywords;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Draft4
{
    public static class SchemaDraft4Setup
    {
        /// <summary>
        /// The draft version key used for keyed service registration.
        /// Note: Draft 4 uses a fragment identifier (#) in the URI.
        /// </summary>
        public const string DraftVersion = "http://json-schema.org/draft-04/schema";

        public static IServiceCollection AddDraft4(this IServiceCollection services)
        {
            services.AddSingleton<ISchemaDraftMeta, SchemaDraft4Meta>();
            services.AddSingleton<ISchemaDraftValidatorFactory, SchemaDraft4ValidatorFactory>();

            // Register keyword validators with keyed services using draft version as key
            // Note: Draft 4 requires boolean schema handling for additionalProperties: false/true etc.
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, BooleanValidatorFactory>(DraftVersion);

            // Core keywords
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, RefValidatorFactory>(DraftVersion);

            // Validation keywords
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, TypeValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, TypeMultipleTypesValidatorFactory>(DraftVersion);
            // Note: const is NOT in Draft 4 (added in Draft 6)
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, EnumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MultipleOfValidatorFactory>(DraftVersion);
            // Note: In Draft 4, exclusiveMaximum/exclusiveMinimum are boolean modifiers for maximum/minimum
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaximumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinimumValidatorFactory>(DraftVersion);
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
            // Note: if/then/else is NOT in Draft 4 (added in Draft 7)
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PatternPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AdditionalPropertiesValidatorFactory>(DraftVersion);
            // Note: propertyNames is NOT in Draft 4 (added in Draft 6)
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, DependenciesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AdditionalItemsValidatorFactory>(DraftVersion);
            // Note: contains is NOT in Draft 4 (added in Draft 6)

            // Format keyword
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, FormatValidatorFactory>(DraftVersion);

            return services;
        }
    }
}
