// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 2019-09, Draft 2020-12
// Helper logic for parsing schema values.

using System.Text.Json;
using FormFinch.JsonSchemaValidation.Exceptions;

namespace FormFinch.JsonSchemaValidation.Draft4.Keywords.Logic
{
    internal static class ValueLogic
    {
        public static bool TryGetNonNegativeInteger(this JsonElement schema, string propertyName, out int? result)
        {
            result = -1;

            if (!schema.TryGetProperty(propertyName, out var propertyElement))
            {
                return false;
            }

            if (propertyElement.ValueKind != JsonValueKind.Number)
            {
                return false;
            }

            if (!propertyElement.TryGetDouble(out var doubleValue))
            {
                return false;
            }

            if (doubleValue < 0 || Math.Abs(doubleValue - Math.Floor(doubleValue)) > double.Epsilon || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException($"The '{propertyName}' keyword must have a non-negative integer value.");
            }

            result = (int)doubleValue;
            return true;
        }
    }
}
