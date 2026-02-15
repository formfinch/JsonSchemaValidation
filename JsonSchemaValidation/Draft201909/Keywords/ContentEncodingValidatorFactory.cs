// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 7, Draft 2019-09, Draft 2020-12
// Factory for contentEncoding annotation-only validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    /// <summary>
    /// Factory for the contentEncoding keyword.
    /// Per JSON Schema Draft 2019-09, contentEncoding is an annotation-only keyword
    /// that describes the encoding (e.g., base64) of a string value.
    /// It does not perform validation - only provides annotations.
    /// </summary>
    internal class ContentEncodingValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "contentEncoding";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("contentEncoding", out var contentEncodingElement))
            {
                return null;
            }

            // contentEncoding must be a string per the spec
            if (contentEncodingElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var encoding = contentEncodingElement.GetString();
            if (string.IsNullOrEmpty(encoding))
            {
                return null;
            }

            return new ContentEncodingValidator(encoding);
        }
    }
}
