using System.Text.Json;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    public interface ISchemaRepository
    {
        bool TryRegisterSchema(JsonElement? schemaToRegister, out SchemaMetadata? schemaData);
        bool TryRegisterSchema(JsonElement? schemaToRegister, Uri schemaUri, out SchemaMetadata? schemaData);
        SchemaMetadata GetSchema(Uri schemaUri, bool dynamicRef = false);
        bool TryGetDynamicRef(string dynamicAnchor, out SchemaMetadata? result);
    }
}
