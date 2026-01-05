using System.Text.Json;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Common
{
    public class JsonValidationArrayContext : JsonValidationContext, IJsonValidationArrayContext
    {
        public struct Annotations
        {
            public int EvaluatedIndex = -1;
            public HashSet<int> EvaluatedIndices = new();
            public bool ItemsEvaluated = false;
            public bool AdditionalItemsEvaluated = false;
            public bool UnevaluatedItemsEvaluated = false;

            public Annotations()
            {
            }
        };

        public Annotations _current = new();

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
            foreach (int idx in indices)
            {
                _current.EvaluatedIndices.Add(idx);
            }
        }
    }
}
