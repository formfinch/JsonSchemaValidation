// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Factory for the contentMediaType keyword.
    /// Per JSON Schema Draft 2020-12, contentMediaType is an annotation-only keyword
    /// that describes the media type (e.g., application/json) of a string value.
    /// It does not perform validation - only provides annotations.
    /// </summary>
    internal class ContentMediaTypeValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "contentMediaType";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("contentMediaType", out var contentMediaTypeElement))
            {
                return null;
            }

            // contentMediaType must be a string per the spec
            if (contentMediaTypeElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var mediaType = contentMediaTypeElement.GetString();
            if (string.IsNullOrEmpty(mediaType))
            {
                return null;
            }

            return new ContentMediaTypeValidator(mediaType);
        }
    }
}
