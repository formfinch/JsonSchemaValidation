// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Common
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
            // Convert to Annotations struct for compatibility - inline logic to avoid IEnumerable allocation
            var annotations = new JsonValidationObjectContext.Annotations();
            if (_data.ValueKind != JsonValueKind.Object)
                return annotations;

            var enumerator = _data.EnumerateObject();
            if (_evaluatedProperties == null || _evaluatedProperties.Count == 0)
            {
                // Nothing evaluated yet - add all properties
                while (enumerator.MoveNext())
                {
                    annotations.UnEvaluatedProperties[enumerator.Current.Name] = enumerator.Current;
                }
            }
            else
            {
                // Filter out evaluated properties
                while (enumerator.MoveNext())
                {
                    if (!_evaluatedProperties.Contains(enumerator.Current.Name))
                    {
                        annotations.UnEvaluatedProperties[enumerator.Current.Name] = enumerator.Current;
                    }
                }
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
            while (enumerator.MoveNext())
            {
                if (!annotations.UnEvaluatedProperties.ContainsKey(enumerator.Current.Name))
                {
                    _evaluatedProperties.Add(enumerator.Current.Name);
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
