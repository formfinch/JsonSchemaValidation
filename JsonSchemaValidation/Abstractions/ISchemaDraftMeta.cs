// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    internal interface ISchemaDraftMeta
    {
        string DraftVersion { get; }
        IEnumerable<JsonElement> Schemas { get; }
    }
}
