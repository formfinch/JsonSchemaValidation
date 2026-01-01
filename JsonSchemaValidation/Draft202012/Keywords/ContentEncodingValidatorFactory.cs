using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    /// <summary>
    /// Factory for the contentEncoding keyword.
    /// Per JSON Schema Draft 2020-12, contentEncoding is an annotation-only keyword
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
