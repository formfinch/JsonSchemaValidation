// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    internal interface ISchemaDraftValidatorFactory
    {
        string DraftVersion { get; }
        ISchemaValidator CreateValidator(SchemaMetadata schemaData);
    }
}
