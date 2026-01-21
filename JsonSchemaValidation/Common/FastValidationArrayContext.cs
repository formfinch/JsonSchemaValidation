// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Common
{
    /// <summary>
    /// Lightweight validation context for arrays in the IsValid() fast path.
    /// Supports item tracking with lazy initialization.
    /// </summary>
    internal sealed class FastValidationArrayContext : IJsonValidationContext, IJsonValidationArrayContext
    {
        private readonly JsonElement _data;
        private readonly IValidationScope _scope;

        private int _evaluatedIndex = -1;
        private HashSet<int>? _evaluatedIndices;
        private bool _itemsEvaluated;
        private bool _additionalItemsEvaluated;
        private bool _unevaluatedItemsEvaluated;

        public FastValidationArrayContext(JsonElement data, IValidationScope scope)
        {
            _data = data;
            _scope = scope;
        }

        public JsonElement Data => _data;

        public IValidationScope Scope => _scope;

        public JsonPointer InstanceLocation => JsonPointer.Empty;

        public IEnumerable<JsonElement> GetUnevaluatedItems()
        {
            if (_data.ValueKind != JsonValueKind.Array)
                yield break;

            if (_itemsEvaluated || _additionalItemsEvaluated || _unevaluatedItemsEvaluated)
                yield break;

            int arrayLength = _data.GetArrayLength();
            for (int idx = _evaluatedIndex + 1; idx < arrayLength; idx++)
            {
                if (_evaluatedIndices == null || !_evaluatedIndices.Contains(idx))
                {
                    yield return _data[idx];
                }
            }
        }

        public void SetEvaluatedIndex(int itemIndex)
        {
            if (_evaluatedIndex < itemIndex)
            {
                _evaluatedIndex = itemIndex;
            }
        }

        public void SetEvaluatedIndices(IEnumerable<int> indices)
        {
            _evaluatedIndices ??= [.. indices];
        }

        public void SetAllItemsEvaluated()
        {
            _itemsEvaluated = true;
        }

        public void SetAdditonalItemsEvaluated()
        {
            _additionalItemsEvaluated = true;
        }

        public void SetUnevaluatedItemsEvaluated()
        {
            _unevaluatedItemsEvaluated = true;
        }

        public JsonValidationArrayContext.Annotations GetAnnotations()
        {
            return new JsonValidationArrayContext.Annotations
            {
                EvaluatedIndex = _evaluatedIndex,
                EvaluatedIndices = _evaluatedIndices ?? new HashSet<int>(),
                ItemsEvaluated = _itemsEvaluated,
                AdditionalItemsEvaluated = _additionalItemsEvaluated,
                UnevaluatedItemsEvaluated = _unevaluatedItemsEvaluated
            };
        }

        public void SetAnnotations(JsonValidationArrayContext.Annotations annotations)
        {
            SetEvaluatedIndex(annotations.EvaluatedIndex);

            if (annotations.ItemsEvaluated)
                SetAllItemsEvaluated();

            if (annotations.AdditionalItemsEvaluated)
                SetAdditonalItemsEvaluated();

            if (annotations.UnevaluatedItemsEvaluated)
                SetUnevaluatedItemsEvaluated();

            if (annotations.EvaluatedIndices.Count > 0)
                SetEvaluatedIndices(annotations.EvaluatedIndices);
        }
    }
}
