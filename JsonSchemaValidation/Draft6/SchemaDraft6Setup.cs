// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common.Keywords;
using FormFinch.JsonSchemaValidation.Draft6.Keywords;
using Microsoft.Extensions.DependencyInjection;

namespace FormFinch.JsonSchemaValidation.Draft6
{
    internal static class SchemaDraft6Setup
    {
        /// <summary>
        /// The draft version key used for keyed service registration.
        /// </summary>
        public const string DraftVersion = "http://json-schema.org/draft-06/schema";

        public static IServiceCollection AddDraft6(this IServiceCollection services)
        {
            services.AddSingleton<ISchemaDraftMeta, SchemaDraft6Meta>();
            services.AddSingleton<ISchemaDraftValidatorFactory, SchemaDraft6ValidatorFactory>();

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
            // Note: if/then/else is NOT in Draft 6 (added in Draft 7)
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

            // Note: contentEncoding and contentMediaType are NOT in Draft 6 (added in Draft 7)

            // Annotation-only keywords
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(DraftVersion, new AnnotationKeywordValidatorFactory("title"));
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(DraftVersion, new AnnotationKeywordValidatorFactory("description"));
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(DraftVersion, new AnnotationKeywordValidatorFactory("default"));
            services.AddKeyedSingleton<ISchemaDraftKeywordValidatorFactory>(DraftVersion, new AnnotationKeywordValidatorFactory("examples"));

            return services;
        }
    }
}
