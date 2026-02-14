// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Common.Keywords
{
    /// <summary>
    /// Generic factory for annotation-only keywords.
    /// Parameterized by keyword name; extracts the value from the schema and creates
    /// an <see cref="AnnotationKeywordValidator"/> that emits it as an annotation.
    /// </summary>
    internal sealed class AnnotationKeywordValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly string _keyword;

        public string Keyword => _keyword;

        public bool IgnoreVocabularyFilter { get; }

        public AnnotationKeywordValidatorFactory(string keyword, bool ignoreVocabularyFilter = false)
        {
            _keyword = keyword;
            IgnoreVocabularyFilter = ignoreVocabularyFilter;
        }

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty(_keyword, out var element))
            {
                return null;
            }

            var value = ExtractValue(element);
            return new AnnotationKeywordValidator(_keyword, value);
        }

        private static object? ExtractValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                // Arrays, objects, numbers: clone to keep the value alive after the document is disposed
                _ => element.Clone()
            };
        }
    }
}
