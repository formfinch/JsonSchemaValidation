using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation;

namespace FormFinch.JsonSchemaValidation.Draft202012.Keywords
{
    internal sealed class PropertyNamesValidator : IKeywordValidator
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "propertyNames";

        public PropertyNamesValidator(ISchemaValidator validator, IJsonValidationContextFactory contextFactory)
        {
            _validator = validator;
            _contextFactory = contextFactory;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            if (context.Data.ValueKind != JsonValueKind.Object)
            {
                // If the instance is not an object, it's considered valid with respect to the properties keyword
                return ValidationResult.Valid(instanceLocation, kwLocation);
            }

            var children = new List<ValidationResult>();
            var invalidPropertyNames = new List<string>();

#pragma warning disable S3267 // Loop has side effects (validation calls)
            foreach (var prp in context.Data.EnumerateObject())
#pragma warning restore S3267
            {
                string jsonString = $"{{\"key\": \"{JsonEncodedText.Encode(prp.Name).ToString()}\"}}";
                using JsonDocument doc = JsonDocument.Parse(jsonString);
                var prpValue = doc.RootElement.GetProperty("key");
                var prpContext = _contextFactory.CreateContextForProperty(context, prp.Name, prpValue);
                var validationResult = _validator.Validate(prpContext, keywordLocation);
                children.Add(validationResult);

                if (!validationResult.IsValid)
                {
                    invalidPropertyNames.Add(prp.Name);
                }
            }

            if (invalidPropertyNames.Count > 0)
            {
                var names = string.Join(", ", invalidPropertyNames.Select(n => $"'{n}'"));
                return ValidationResult.Invalid(instanceLocation, kwLocation, $"Property names are invalid: {names}") with { Children = children };
            }

            return ValidationResult.Valid(instanceLocation, kwLocation) with { Children = children.Count > 0 ? children : null };
        }
    }
}
