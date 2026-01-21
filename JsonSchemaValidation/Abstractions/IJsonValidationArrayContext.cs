// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
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
