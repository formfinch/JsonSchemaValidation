using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Draft3.Keywords;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Draft3
{
    public static class SchemaDraft3Setup
    {
        /// <summary>
        /// The draft version key used for keyed service registration.
        /// Note: Draft 3 uses a fragment identifier (#) in the URI.
        /// </summary>
        public const string DraftVersion = "http://json-schema.org/draft-03/schema";

        public static IServiceCollection AddDraft3(this IServiceCollection services)
        {
            services.AddSingleton<ISchemaDraftMeta, SchemaDraft3Meta>();
            services.AddSingleton<ISchemaDraftValidatorFactory, SchemaDraft3ValidatorFactory>();

            // Register keyword validators with keyed services using draft version as key
            // Note: Draft 3 requires boolean schema handling for additionalProperties: false/true etc.
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, BooleanValidatorFactory>(DraftVersion);

            // Core keywords
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, RefValidatorFactory>(DraftVersion);

            // Validation keywords
            // Note: Draft 3 type supports "any" and can include schemas in the array
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, TypeValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, TypeMultipleTypesValidatorFactory>(DraftVersion);
            // Note: const is NOT in Draft 3
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, EnumValidatorFactory>(DraftVersion);
            // Note: Draft 3 uses divisibleBy instead of multipleOf
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, DivisibleByValidatorFactory>(DraftVersion);
            // Note: In Draft 3, exclusiveMaximum/exclusiveMinimum are boolean modifiers for maximum/minimum
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaximumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinimumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaxLengthValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinLengthValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PatternValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaxItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, UniqueItemsValidatorFactory>(DraftVersion);
            // Note: maxProperties/minProperties are NOT in Draft 3
            // Note: Draft 3 required is handled by PropertiesValidatorFactory (boolean on property definition)

            // Applicator keywords
            // Note: allOf, anyOf, oneOf, not are NOT in Draft 3
            // Draft 3 uses extends instead of allOf
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ExtendsValidatorFactory>(DraftVersion);
            // Note: Draft 3 uses disallow keyword (inverse of type)
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, DisallowValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PatternPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AdditionalPropertiesValidatorFactory>(DraftVersion);
            // Note: propertyNames is NOT in Draft 3
            // Note: Draft 3 dependencies supports string, array, or schema
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, DependenciesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AdditionalItemsValidatorFactory>(DraftVersion);
            // Note: contains is NOT in Draft 3

            // Format keyword
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, FormatValidatorFactory>(DraftVersion);

            return services;
        }
    }
}
