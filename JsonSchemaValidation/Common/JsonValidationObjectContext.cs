using System.Text.Json;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Common
{
    public class JsonValidationObjectContext : JsonValidationContext, IJsonValidationObjectContext
    {
        public struct Annotations
        {
            public Dictionary<string, JsonProperty> UnEvaluatedProperties = new();

            public Annotations()
            {
            }
        };

        public Annotations _current = new();

        public JsonValidationObjectContext(JsonElement data) : base(data)
        {
            InitializeUnevaluatedProperties(data);
        }

        public JsonValidationObjectContext(JsonElement data, IValidationScope scope) : base(data, scope)
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
            if(Data.ValueKind != JsonValueKind.Object)
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
            foreach(var key in _current.UnEvaluatedProperties.Keys)
            {
                if(!annotations.UnEvaluatedProperties.ContainsKey(key))
                {
                    MarkPropertyEvaluated(key);
                }
            }
        }

        public void SetUnevaluatedPropertiesEvaluated()
        {
            _current.UnEvaluatedProperties.Clear();
        }

    }
}
