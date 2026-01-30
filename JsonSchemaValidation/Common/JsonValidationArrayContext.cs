// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Common
{
    internal class JsonValidationArrayContext : JsonValidationContext, IJsonValidationArrayContext
    {
        public struct Annotations
        {
            public int EvaluatedIndex { get; set; } = -1;
            public ISet<int> EvaluatedIndices { get; set; } = new HashSet<int>();
            public bool ItemsEvaluated { get; set; }
            public bool AdditionalItemsEvaluated { get; set; }
            public bool UnevaluatedItemsEvaluated { get; set; }

            public Annotations()
            {
            }
        }

        private Annotations _current = new();

        public JsonValidationArrayContext(JsonElement data) : base(data) { }

        public JsonValidationArrayContext(JsonElement data, IValidationScope scope) : base(data, scope) { }

        public JsonValidationArrayContext(JsonElement data, IValidationScope scope, JsonPointer instanceLocation)
            : base(data, scope, instanceLocation) { }

        public IEnumerable<JsonElement> GetUnevaluatedItems()
        {
            if (!_current.ItemsEvaluated && !_current.AdditionalItemsEvaluated && !_current.UnevaluatedItemsEvaluated)
            {
                for (int idx = _current.EvaluatedIndex + 1; idx < this.Data.GetArrayLength(); idx++)
                {
                    if (!_current.EvaluatedIndices.Contains(idx))
                    {
                        yield return this.Data[idx];
                    }
                }
            }
        }

        public Annotations GetAnnotations()
        {
            return _current;
        }

        public void SetAnnotations(Annotations annotations)
        {
            SetEvaluatedIndex(annotations.EvaluatedIndex);

            if (annotations.ItemsEvaluated)
            {
                SetAllItemsEvaluated();
            }

            if (annotations.AdditionalItemsEvaluated)
            {
                SetAdditonalItemsEvaluated();
            }

            if (annotations.UnevaluatedItemsEvaluated)
            {
                SetUnevaluatedItemsEvaluated();
            }

            if (annotations.EvaluatedIndices.Any())
            {
                SetEvaluatedIndices(annotations.EvaluatedIndices);
            }
        }

        public void SetEvaluatedIndex(int itemIndex)
        {
            if (_current.EvaluatedIndex < itemIndex)
            {
                _current.EvaluatedIndex = itemIndex;
            }
        }

        public void SetAllItemsEvaluated()
        {
            _current.ItemsEvaluated = true;
        }

        public void SetAdditonalItemsEvaluated()
        {
            _current.AdditionalItemsEvaluated = true;
        }

        public void SetUnevaluatedItemsEvaluated()
        {
            _current.UnevaluatedItemsEvaluated = true;
        }

        public void SetEvaluatedIndices(IEnumerable<int> indices)
        {
            // Use index-based iteration to avoid enumerator allocation with IEnumerable
            if (indices is IList<int> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    _current.EvaluatedIndices.Add(list[i]);
                }
            }
            else if (indices is IReadOnlyList<int> readOnlyList)
            {
                for (int i = 0; i < readOnlyList.Count; i++)
                {
                    _current.EvaluatedIndices.Add(readOnlyList[i]);
                }
            }
            else
            {
                // Fallback: materialize to array and iterate
                int[] indicesArray = indices.ToArray();
                for (int i = 0; i < indicesArray.Length; i++)
                {
                    _current.EvaluatedIndices.Add(indicesArray[i]);
                }
            }
        }
    }
}
