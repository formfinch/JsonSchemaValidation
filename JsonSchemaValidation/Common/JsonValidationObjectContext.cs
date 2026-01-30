// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Common
{
    internal class JsonValidationObjectContext : JsonValidationContext, IJsonValidationObjectContext
    {
        public struct Annotations
        {
            public Dictionary<string, JsonProperty> UnEvaluatedProperties { get; set; } = new Dictionary<string, JsonProperty>(StringComparer.Ordinal);

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
            List<string>? keysToRemove = null;
            var enumerator = _current.UnEvaluatedProperties.Keys.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (!annotations.UnEvaluatedProperties.ContainsKey(enumerator.Current))
                {
                    keysToRemove ??= [];
                    keysToRemove.Add(enumerator.Current);
                }
            }

            if (keysToRemove != null)
            {
                foreach (var key in keysToRemove)
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
