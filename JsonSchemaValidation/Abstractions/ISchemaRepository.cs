// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    internal interface ISchemaRepository
    {
        bool TryRegisterSchema(JsonElement? schemaToRegister, out SchemaMetadata? schemaData);
        bool TryRegisterSchema(JsonElement? schemaToRegister, Uri schemaUri, out SchemaMetadata? schemaData);
        SchemaMetadata GetSchema(Uri schemaUri, bool dynamicRef = false);
        bool TryGetDynamicRef(string dynamicAnchor, out SchemaMetadata? result);
    }
}
