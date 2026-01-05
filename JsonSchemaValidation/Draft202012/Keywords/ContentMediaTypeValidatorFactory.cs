using System.Text.Json;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Draft202012.Keywords
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
