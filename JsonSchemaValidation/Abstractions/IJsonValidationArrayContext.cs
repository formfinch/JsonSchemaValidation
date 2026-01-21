using System.Text.Json;
using static FormFinch.JsonSchemaValidation.Common.JsonValidationArrayContext;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    public interface IJsonValidationArrayContext
    {
        IEnumerable<JsonElement> GetUnevaluatedItems();
        void SetAdditonalItemsEvaluated();
        void SetEvaluatedIndex(int itemIndex);
        void SetEvaluatedIndices(IEnumerable<int> indices);
        void SetAllItemsEvaluated();
        void SetUnevaluatedItemsEvaluated();

        Annotations GetAnnotations();
        void SetAnnotations(Annotations annotations);
    }
}
