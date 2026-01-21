using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Draft202012.Keywords;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Draft202012
{
    public static class SchemaDraft202012Setup
    {
        /// <summary>
        /// The draft version key used for keyed service registration.
        /// </summary>
        public const string DraftVersion = "https://json-schema.org/draft/2020-12/schema";

        public static IServiceCollection AddDraft202012(this IServiceCollection services)
        {
            // Vocabulary support (each parser owns its own registry)
            services.AddSingleton<IVocabularyParser, VocabularyParser>();

            services.AddSingleton<ISchemaDraftMeta, SchemaDraft202012Meta>();
            services.AddSingleton<ISchemaDraftValidatorFactory, SchemaDraft202012ValidatorFactory>();

            // Register keyword validators with keyed services using draft version as key
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AdditionalPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AllOfValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, AnyOfValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, BooleanValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ContainsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ConstValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ContentEncodingValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ContentMediaTypeValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ContentSchemaValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, DependentRequiredValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, DependentSchemasValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, DynamicRefValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, RefValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, EnumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ExclusiveMaximumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ExclusiveMinimumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, FormatValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, IfThenElseValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, ItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaximumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaxItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaxLengthValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MaxPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinimumValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinLengthValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MinPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, MultipleOfValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, NotValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, OneOfValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PatternValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PatternPropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PrefixItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PropertiesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, PropertyNamesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, RequiredValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, TypeValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, TypeMultipleTypesValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, UniqueItemsValidatorFactory>(DraftVersion);

            // Unevaluated validators have ExecutionOrder=100 to ensure they run last
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, UnevaluatedItemsValidatorFactory>(DraftVersion);
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory, UnevaluatedPropertiesValidatorFactory>(DraftVersion);

            return services;
        }
    }
}
