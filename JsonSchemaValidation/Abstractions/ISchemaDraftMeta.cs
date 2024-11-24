using System.Text.Json;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaDraftMeta
    {
        string DraftVersion { get; }
        IEnumerable<JsonElement> Schemas { get; }
    }
}
