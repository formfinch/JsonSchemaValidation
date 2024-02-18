using JsonSchemaValidation.Repositories;
using System.Text.Json;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaRepository
    {
        bool TryRegisterSchema(JsonElement? schemaToRegister, out SchemaMetadata? schemaData);
        SchemaMetadata GetSchema(Uri schemaUri, bool dynamicRef = false);
        bool TryGetDynamicRef(string dynamicAnchor, out SchemaMetadata? result);
    }
}