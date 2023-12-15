using System.Text.Json;

namespace JsonSchemaValidation.Abstractions
{
    public interface IJsonValidationContext
    {
        JsonElement Data { get; }
    }
}