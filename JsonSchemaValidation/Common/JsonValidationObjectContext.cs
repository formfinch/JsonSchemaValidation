using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Common
{
    public class JsonValidationObjectContext : JsonValidationContext, IJsonValidationObjectContext
    {
        public struct Annotations
        {
            public IDictionary<string, JsonProperty> UnEvaluatedProperties { get; set; } = new Dictionary<string, JsonProperty>(StringComparer.Ordinal);

            public Annotations()
            {
            }
        }

        private Annotations _current = new();

        public JsonValidationObjectContext(JsonElement data) : base(data)
        {
            InitializeUnevaluatedProperties(data);
        }

        public JsonValidationObjectContext(JsonElement data, IValidationScope scope) : base(data, scope)
        {
            InitializeUnevaluatedProperties(data);
        }

        public JsonValidationObjectContext(JsonElement data, IValidationScope scope, JsonPointer instanceLocation)
            : base(data, scope, instanceLocation)
        {
            InitializeUnevaluatedProperties(data);
        }

        private void InitializeUnevaluatedProperties(JsonElement data)
        {
            if (Data.ValueKind == JsonValueKind.Object)
            {
                foreach (var prp in data.EnumerateObject())
                {
                    _current.UnEvaluatedProperties.Add(prp.Name, prp);
                }
            }
        }

        public Annotations GetAnnotations()
        {
            return _current;
        }

        public IEnumerable<JsonProperty> GetUnevaluatedProperties()
        {
            if (Data.ValueKind != JsonValueKind.Object)
            {
                return Enumerable.Empty<JsonProperty>();
            }

            return _current.UnEvaluatedProperties.Values;
        }

        public void MarkPropertyEvaluated(string propertyName)
        {
            _current.UnEvaluatedProperties.Remove(propertyName);
        }

        public void SetAnnotations(Annotations annotations)
        {
            // Collect keys to remove first to avoid modifying during enumeration
            var keysToRemove = new List<string>();
            var keys = _current.UnEvaluatedProperties.Keys;
            for (int i = 0; keys.Skip(i).Any(); i++)
            {
                var key = keys.ElementAt(i);
                if (!annotations.UnEvaluatedProperties.ContainsKey(key))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                MarkPropertyEvaluated(key);
            }
        }

        public void SetUnevaluatedPropertiesEvaluated()
        {
            _current.UnEvaluatedProperties.Clear();
        }

    }
}
