using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    public interface ISchemaDraftMeta
    {
        string DraftVersion { get; }
        IEnumerable<JsonElement> Schemas { get; }
    }
}
