using System.Text.Json;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Common
{
    /// <summary>
    /// Lightweight validation context for objects in the IsValid() fast path.
    /// Supports property tracking via lazy HashSet instead of eager Dictionary.
    /// </summary>
    internal sealed class FastValidationObjectContext : IJsonValidationContext, IJsonValidationObjectContext
    {
        private readonly JsonElement _data;
        private readonly IValidationScope _scope;
        private HashSet<string>? _evaluatedProperties;

        public FastValidationObjectContext(JsonElement data, IValidationScope scope)
        {
            _data = data;
            _scope = scope;
        }

        public JsonElement Data => _data;

        public IValidationScope Scope => _scope;

        public JsonPointer InstanceLocation => JsonPointer.Empty;

        public void MarkPropertyEvaluated(string propertyName)
        {
            _evaluatedProperties ??= new HashSet<string>(StringComparer.Ordinal);
            _evaluatedProperties.Add(propertyName);
        }

        public IEnumerable<JsonProperty> GetUnevaluatedProperties()
        {
            if (_data.ValueKind != JsonValueKind.Object)
                yield break;

            if (_evaluatedProperties == null || _evaluatedProperties.Count == 0)
            {
                // Nothing evaluated yet - return all properties
                foreach (var prp in _data.EnumerateObject())
                    yield return prp;
            }
            else
            {
                // Filter out evaluated properties
                foreach (var prp in _data.EnumerateObject())
                {
                    if (!_evaluatedProperties.Contains(prp.Name))
                        yield return prp;
                }
            }
        }

        public JsonValidationObjectContext.Annotations GetAnnotations()
        {
            // Convert to Annotations struct for compatibility
            var annotations = new JsonValidationObjectContext.Annotations();
            var unEvaluatedProperties = GetUnevaluatedProperties();
            for(int i = 0; unEvaluatedProperties.Skip(i).Any(); i++)
            {
                var prp = unEvaluatedProperties.ElementAt(i);
                annotations.UnEvaluatedProperties[prp.Name] = prp;
            }
            return annotations;
        }

        public void SetAnnotations(JsonValidationObjectContext.Annotations annotations)
        {
            // Mark properties as evaluated if they're not in the source annotations
            if (_data.ValueKind != JsonValueKind.Object)
                return;

            _evaluatedProperties ??= new HashSet<string>(StringComparer.Ordinal);
            var enumerator = _data.EnumerateObject();
            for (int i = 0; enumerator.Skip(i).Any(); i++)
            {
                var prp = enumerator.ElementAt(i);
                if (!annotations.UnEvaluatedProperties.ContainsKey(prp.Name))
                {
                    _evaluatedProperties.Add(prp.Name);
                }
            }
        }

        public void SetUnevaluatedPropertiesEvaluated()
        {
            // Mark all properties as evaluated
            _evaluatedProperties ??= new HashSet<string>(StringComparer.Ordinal);
            foreach (var prp in _data.EnumerateObject())
            {
                _evaluatedProperties.Add(prp.Name);
            }
        }
    }
}
