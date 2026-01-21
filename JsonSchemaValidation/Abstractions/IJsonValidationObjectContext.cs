using System.Text.Json;
using static FormFinch.JsonSchemaValidation.Common.JsonValidationObjectContext;

namespace FormFinch.JsonSchemaValidation.Abstractions
{
    public interface IJsonValidationObjectContext
    {
        Annotations GetAnnotations();
        IEnumerable<JsonProperty> GetUnevaluatedProperties();
        void MarkPropertyEvaluated(string propertyName);
        void SetAnnotations(Annotations annotations);
        void SetUnevaluatedPropertiesEvaluated();
    }
}
