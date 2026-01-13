// Draft 3 behavior: disallow is the inverse of type.
// Validates that data does NOT match any of the disallowed types or schemas.

using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Validation;

namespace JsonSchemaValidation.Draft3.Keywords
{
    internal sealed class DisallowValidator : IKeywordValidator
    {
        private readonly IKeywordValidator[] _typeValidators;
        private readonly ISchemaValidator[] _schemaValidators;
        private readonly IJsonValidationContextFactory _contextFactory;

        public string Keyword => "disallow";

        public bool SupportsDirectValidation => _schemaValidators.Length == 0;

        public DisallowValidator(
            IEnumerable<IKeywordValidator> typeValidators,
            IEnumerable<ISchemaValidator> schemaValidators,
            IJsonValidationContextFactory contextFactory)
        {
            _typeValidators = typeValidators.ToArray();
            _schemaValidators = schemaValidators.ToArray();
            _contextFactory = contextFactory;
        }

        public bool IsValid(JsonElement data)
        {
            // Only use this fast path when there are no schema validators
            if (_schemaValidators.Length > 0)
            {
                throw new InvalidOperationException("Cannot use direct validation when schema validators are present");
            }

            // If data matches ANY disallowed type, it's invalid
#pragma warning disable S3267 // Loop has early return for performance
            foreach (var validator in _typeValidators)
            {
                if (validator.IsValid(data))
                    return false;
            }
#pragma warning restore S3267
            return true;
        }

        public bool IsValid(IJsonValidationContext context)
        {
            // If data matches ANY disallowed type, it's invalid
#pragma warning disable S3267 // Loop has early return for performance
            foreach (var validator in _typeValidators)
            {
                if (validator.IsValid(context.Data))
                    return false;
            }
#pragma warning restore S3267

            // If data matches ANY disallowed schema, it's invalid
            foreach (var validator in _schemaValidators)
            {
                var activeContext = _contextFactory.CreateFreshContextFast(context, false);
                if (validator.IsValid(activeContext))
                    return false;
            }

            return true;
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var instanceLocation = context.InstanceLocation.ToString();
            var kwLocation = keywordLocation.ToString();

            // If data matches ANY disallowed type, it's invalid
#pragma warning disable S3267 // Loop has early return for performance
            foreach (var validator in _typeValidators)
            {
                if (validator.IsValid(context.Data))
                {
                    return ValidationResult.Invalid(instanceLocation, kwLocation, $"Value matches a disallowed type: {validator.Keyword}");
                }
            }
#pragma warning restore S3267

            // If data matches ANY disallowed schema, it's invalid
            int idx = 0;
            foreach (var validator in _schemaValidators)
            {
                var activeContext = _contextFactory.CreateFreshContext(context);
                var subSchemaPath = keywordLocation.Append(idx);
                var schemaResult = validator.Validate(activeContext, subSchemaPath);

                if (schemaResult.IsValid)
                {
                    return ValidationResult.Invalid(instanceLocation, kwLocation, "Value matches a disallowed schema");
                }
                idx++;
            }

            return ValidationResult.Valid(instanceLocation, kwLocation);
        }
    }
}
