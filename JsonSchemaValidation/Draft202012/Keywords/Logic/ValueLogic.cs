using System.Text.Json;
using JsonSchemaValidation.Exceptions;

namespace JsonSchemaValidation.Draft202012.Keywords.Logic
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

            if (doubleValue < 0 || doubleValue != Math.Floor(doubleValue) || doubleValue > int.MaxValue)
            {
                throw new InvalidSchemaException($"The '{propertyName}' keyword must have a non-negative integer value.");
            }

            result = (int)doubleValue;
            return true;
        }
    }
}
