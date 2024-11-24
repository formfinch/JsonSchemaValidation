using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System.Text.Json;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class PropertyNamesValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public PropertyNamesValidator(ISchemaValidator validator, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context)
        {
            if(context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return ValidationResult.Ok;
            }

            ValidationResult result = new ();
            foreach(var prp in context.Data.EnumerateObject())
            {
                string jsonString = $"{{\"key\": \"{JsonEncodedText.Encode(prp.Name)}\"}}";
                using JsonDocument doc = JsonDocument.Parse(jsonString);
                var prpValue = doc.RootElement.GetProperty("key");
                var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, prpValue);
                var validationResult = _validator.Validate(prpContext);
                if(validationResult != ValidationResult.Ok)
                {
                    var propertyNameResult = new ValidationResult($"Property name {prp.Name} is invalid");
                    result.Merge(propertyNameResult);
                }
            }

            if(!result.IsValid)
            {
                return result;
            }
            return ValidationResult.Ok;
        }
    }
}