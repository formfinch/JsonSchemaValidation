// Draft behavior: Identical in Draft 2019-09, Draft 2020-12
// Note: contentSchema was added in Draft 2019-09.
// Factory for contentSchema annotation-only validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    /// <summary>
    /// Factory for the contentSchema keyword.
    /// Per JSON Schema Draft 2019-09, contentSchema is an annotation-only keyword
    /// that describes the schema that the decoded content should conform to.
    /// It does not perform validation - only provides annotations.
    /// </summary>
    internal class ContentSchemaValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "contentSchema";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("contentSchema", out var contentSchemaElement))
            {
                return null;
            }

            // contentSchema must be a valid JSON Schema (object or boolean)
            if (contentSchemaElement.ValueKind != JsonValueKind.Object &&
                contentSchemaElement.ValueKind != JsonValueKind.True &&
                contentSchemaElement.ValueKind != JsonValueKind.False)
            {
                return null;
            }

            return new ContentSchemaValidator(contentSchemaElement);
        }
    }
}
