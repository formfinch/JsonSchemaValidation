using System.Text.Json;
using static JsonSchemaValidation.Common.JsonValidationObjectContext;

namespace JsonSchemaValidation.Abstractions
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
