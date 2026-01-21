using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;
using FormFinch.JsonSchemaValidation.Common;

namespace FormFinch.JsonSchemaValidation.Validation
{
    public sealed class SchemaValidator : ISchemaValidator
    {
        private readonly List<IKeywordValidator> _keywordValidators = new();
        // Cached arrays for fast path to avoid LINQ overhead
        private IKeywordValidator[]? _directValidators;
        private IKeywordValidator[]? _contextValidators;
        private bool _cacheBuilt;
        private bool _requiresAnnotationTracking;

        /// <summary>
        /// Indicates whether this schema requires annotation tracking for correct validation.
        /// Set to true when unevaluatedProperties or unevaluatedItems keywords are present.
        /// </summary>
        public bool RequiresAnnotationTracking => _requiresAnnotationTracking;

        public void AddKeywordValidator(IKeywordValidator keywordValidator)
        {
            _keywordValidators.Add(keywordValidator);
            _cacheBuilt = false;

            // Detect keywords that require annotation tracking
            if (keywordValidator.Keyword is "unevaluatedProperties" or "unevaluatedItems")
            {
                _requiresAnnotationTracking = true;
            }
        }

        public ValidationResult Validate(IJsonValidationContext context, JsonPointer keywordLocation)
        {
            var children = new List<ValidationResult>();

            foreach (var validator in _keywordValidators)
            {
                // Each keyword validator gets its own keyword path: parent + keyword name
                var keywordPath = keywordLocation.Append(validator.Keyword);
                var validatorResult = validator.Validate(context, keywordPath);
                children.Add(validatorResult);
            }

            // Aggregate all keyword results
            return ValidationResult.Aggregate(
                context.InstanceLocation.ToString(),
                keywordLocation.ToString(),
                children
            );
        }

        public bool IsValid(IJsonValidationContext context)
        {
            EnsureCacheBuilt();
            var data = context.Data;

            // Fast path for validators that support direct JsonElement validation
            if (_directValidators != null)
            {
                foreach (var validator in _directValidators)
                {
                    if (!validator.IsValid(data))
                        return false;
                }
            }

            // Context-required validators (refs, applicators that need scope)
            if (_contextValidators != null)
            {
                foreach (var validator in _contextValidators)
                {
                    if (!validator.IsValid(context))
                        return false;
                }
            }

            return true;
        }

        private void EnsureCacheBuilt()
        {
            if (_cacheBuilt)
                return;

            var directList = new List<IKeywordValidator>();
            var contextList = new List<IKeywordValidator>();

            foreach (var validator in _keywordValidators)
            {
                if (validator.SupportsDirectValidation)
                    directList.Add(validator);
                else
                    contextList.Add(validator);
            }

            _directValidators = directList.Count > 0 ? directList.ToArray() : null;
            _contextValidators = contextList.Count > 0 ? contextList.ToArray() : null;
            _cacheBuilt = true;
        }
    }
}
