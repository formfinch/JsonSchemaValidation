// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Common
{
    internal static class JsonElementExtensions
    {
        private static readonly JsonElement nullElement = JsonDocument.Parse("null").RootElement;

        public static JsonElement GetElementByJsonPointer(this JsonElement element, string pointer)
        {
            if (!pointer.StartsWith("#/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Invalid JSON Pointer syntax. Must start with '#/'.", nameof(pointer));
            }

            string[] parts = pointer.Substring(2).Split('/');
            JsonElement currentElement = element;

            foreach (var part in parts)
            {
                string unescapedPart = UnescapeJsonPointer(part);

                if (currentElement.ValueKind == JsonValueKind.Object && currentElement.TryGetProperty(unescapedPart, out var nextElement))
                {
                    currentElement = nextElement;
                }
                else if (currentElement.ValueKind == JsonValueKind.Array && int.TryParse(unescapedPart, System.Globalization.CultureInfo.InvariantCulture, out int index) && index < currentElement.GetArrayLength())
                {
                    currentElement = currentElement[index];
                }
                else
                {
                    throw new InvalidOperationException($"Property or index not found: {unescapedPart}");
                }
            }

            return currentElement;
        }

        private static string UnescapeJsonPointer(string pointerPart)
        {
            // Replaces '~1' with '/' and '~0' with '~', as per the JSON Pointer specification.
            return pointerPart.Replace("~1", "/").Replace("~0", "~");
        }

        public static JsonElement GetArrayProperty(this JsonElement element, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return nullElement;
            }

            if (!element.TryGetProperty(propertyName, out var value))
            {
                return nullElement;
            }

            if (value.ValueKind != JsonValueKind.Array)
            {
                return nullElement;
            }

            return value;
        }

        public static JsonElement GetObjectProperty(this JsonElement element, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return nullElement;
            }

            if (!element.TryGetProperty(propertyName, out var value))
            {
                return nullElement;
            }

            if (value.ValueKind != JsonValueKind.Object)
            {
                return nullElement;
            }

            return value;
        }

        /// <summary>
        /// Attempts to get a non-negative integer from the JsonElement.
        /// </summary>
        /// <param name="element">The JsonElement to parse.</param>
        /// <param name="value">The resulting non-negative integer if successful.</param>
        /// <returns>True if the element is a non-negative integer, false otherwise.</returns>
        public static bool TryGetNonNegativeInteger(this JsonElement element, out int value)
        {
            value = 0;
            if (element.ValueKind != JsonValueKind.Number)
            {
                return false;
            }

            if (!element.TryGetInt32(out int intValue))
            {
                return false;
            }

            if (intValue < 0)
            {
                return false;
            }

            value = intValue;
            return true;
        }

    }
}
