using System.Text.Json;

namespace JsonSchemaValidation.Common
{
    internal static class JsonElementExtensions
    {
        private static readonly JsonElement nullElement = JsonDocument.Parse("null").RootElement;

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

            if(element.ValueKind != JsonValueKind.Object)
            {
                return nullElement;
            }

            if(!element.TryGetProperty(propertyName, out var value))
            {
                return nullElement;
            }

            if(value.ValueKind != JsonValueKind.Object)
            {
                return nullElement;
            }

            return value;
        }
    }
}
