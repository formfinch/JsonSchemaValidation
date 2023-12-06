using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Common
{
    internal static class JsonElementExtensions
    {
        public static JsonElement GetElementByJsonPointer(this JsonElement element, string pointer)
        {
            if (!pointer.StartsWith("#/"))
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
                else if (currentElement.ValueKind == JsonValueKind.Array && int.TryParse(unescapedPart, out int index) && index < currentElement.GetArrayLength())
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
    }
}
