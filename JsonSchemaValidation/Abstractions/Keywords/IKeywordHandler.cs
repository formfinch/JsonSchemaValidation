using JsonSchemaValidation.Common;
using System.Text.Json;

namespace JsonSchemaValidation.Abstractions.Keywords
{
    public interface IKeywordHandler
    {
        ValidationResult Validate(JsonElement schemaKeyword, JsonElement jsonData);
    }
}
