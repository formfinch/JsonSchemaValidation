// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// Factory for pattern keyword validator.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Exceptions;
using FormFinch.JsonSchemaValidation.Repositories;

namespace FormFinch.JsonSchemaValidation.Draft4.Keywords
{
    internal class PatternValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        public string Keyword => "pattern";

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("pattern", out var patternElement))
            {
                return null;
            }

            if (patternElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? pattern = patternElement.GetString();
            if (string.IsNullOrEmpty(pattern))
            {
                throw new InvalidSchemaException("The value of this pattern must be a string.");
            }

            var rxPattern = EcmaScriptRegexHelper.CreateEcmaScriptRegex(pattern);
            return new PatternValidator(rxPattern);
        }
    }
}
