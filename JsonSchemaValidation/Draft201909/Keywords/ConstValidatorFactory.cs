// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// The const keyword validates that data equals exactly the specified value.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft201909.Keywords
{
    internal class ConstValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "const";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("const", out var constElement))
            {
                return null;
            }

            if (constElement.ValueKind == JsonValueKind.Undefined)
            {
                throw new InvalidSchemaException("const cannot be undefined.");
            }

            return new ConstValidator(constElement);
        }
    }
}
