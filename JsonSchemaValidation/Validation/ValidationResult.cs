using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation.Output;

namespace FormFinch.JsonSchemaValidation.Validation
{
    /// <summary>
    /// Structured validation result per JSON Schema 2020-12 Section 12.
    /// Immutable - use factory methods to create instances.
    /// </summary>
    public record ValidationResult
    {
        /// <summary>
        /// Whether this validation passed.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// JSON Pointer to the instance location being validated.
        /// </summary>
        public string InstanceLocation { get; }

        /// <summary>
        /// JSON Pointer to the schema keyword that produced this result.
        /// </summary>
        public string KeywordLocation { get; }

        /// <summary>
        /// Absolute URI of the schema keyword including fragment.
        /// </summary>
        public string? AbsoluteKeywordLocation { get; init; }

        /// <summary>
        /// Error message when IsValid is false.
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// Keyword that produced this result (e.g., "minimum", "properties").
        /// </summary>
        public string? Keyword { get; init; }

        /// <summary>
        /// Annotations produced by this keyword.
        /// </summary>
        public IReadOnlyDictionary<string, object?>? Annotations { get; init; }

        /// <summary>
        /// Child validation results for hierarchical output.
        /// </summary>
        public IReadOnlyList<ValidationResult>? Children { get; init; }

        private ValidationResult(bool isValid, string instanceLocation, string keywordLocation, string? error)
        {
            IsValid = isValid;
            InstanceLocation = instanceLocation;
            KeywordLocation = keywordLocation;
            Error = error;
        }

        /// <summary>
        /// Creates a copy of a result with an absolute keyword location set from a schema URI.
        /// </summary>
        public ValidationResult(ValidationResult source, Uri schemaUri)
        {
            IsValid = source.IsValid;
            InstanceLocation = source.InstanceLocation;
            KeywordLocation = source.KeywordLocation;
            AbsoluteKeywordLocation = schemaUri + "#" + source.KeywordLocation;
            Error = source.Error;
            Keyword = source.Keyword;
            Annotations = source.Annotations;
            Children = source.Children;
        }

        /// <summary>
        /// Creates a valid result for the given locations.
        /// </summary>
        public static ValidationResult Valid(string instanceLocation, string keywordLocation)
        {
            return new ValidationResult(true, instanceLocation, keywordLocation, null);
        }

        /// <summary>
        /// Creates a valid result with annotations.
        /// </summary>
        public static ValidationResult Valid(string instanceLocation, string keywordLocation, IReadOnlyDictionary<string, object?> annotations)
        {
            return new ValidationResult(true, instanceLocation, keywordLocation, null)
            {
                Annotations = annotations
            };
        }

        /// <summary>
        /// Creates an invalid result with an error message.
        /// </summary>
        public static ValidationResult Invalid(string instanceLocation, string keywordLocation, string error)
        {
            return new ValidationResult(false, instanceLocation, keywordLocation, error);
        }

        /// <summary>
        /// Creates a result with child results. Overall validity is determined by children.
        /// </summary>
        public static ValidationResult Aggregate(string instanceLocation, string keywordLocation, IEnumerable<ValidationResult> children)
        {
            var childList = children as IReadOnlyList<ValidationResult> ?? children.ToArray();
            var isValid = childList.All(c => c.IsValid);
            return new ValidationResult(isValid, instanceLocation, keywordLocation, null)
            {
                Children = childList
            };
        }

        /// <summary>
        /// Converts this result to an OutputUnit for the specified format.
        /// </summary>
        public OutputUnit ToOutputUnit(OutputFormat format)
        {
            return format switch
            {
                OutputFormat.Flag => ToFlagOutput(),
                OutputFormat.Basic => ToBasicOutput(),
                OutputFormat.Detailed => ToDetailedOutput(),
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }

        private OutputUnit ToFlagOutput()
        {
            return new OutputUnit
            {
                Valid = IsValid,
                InstanceLocation = "",
                KeywordLocation = ""
            };
        }

        private OutputUnit ToBasicOutput()
        {
            var output = new OutputUnit
            {
                Valid = IsValid,
                InstanceLocation = InstanceLocation,
                KeywordLocation = KeywordLocation,
                AbsoluteKeywordLocation = AbsoluteKeywordLocation
            };

            if (!IsValid)
            {
                var errors = new List<OutputUnit>();
                CollectErrorsFlat(errors);
                output.Errors = errors;
                output.Error = GetSummaryError(errors.Count);
            }

            return output;
        }

        private void CollectErrorsFlat(List<OutputUnit> errors)
        {
            if (!IsValid && Error != null)
            {
                errors.Add(new OutputUnit
                {
                    Valid = false,
                    InstanceLocation = InstanceLocation,
                    KeywordLocation = KeywordLocation,
                    AbsoluteKeywordLocation = AbsoluteKeywordLocation,
                    Error = Error
                });
            }

            if (Children != null)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    Children[i].CollectErrorsFlat(errors);
                }
            }
        }

        private OutputUnit ToDetailedOutput()
        {
            var output = new OutputUnit
            {
                Valid = IsValid,
                InstanceLocation = InstanceLocation,
                KeywordLocation = KeywordLocation,
                AbsoluteKeywordLocation = AbsoluteKeywordLocation
            };

            if (Children != null && Children.Count > 0)
            {
                // Single pass through children to partition into errors and annotations
                List<OutputUnit>? childErrors = null;
                List<OutputUnit>? childAnnotations = null;

                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    if (!child.IsValid)
                    {
                        childErrors ??= new List<OutputUnit>();
                        childErrors.Add(child.ToDetailedOutput());
                    }
                    else if (child.Annotations != null)
                    {
                        childAnnotations ??= new List<OutputUnit>();
                        childAnnotations.Add(child.ToDetailedOutput());
                    }
                }

                if (childErrors != null)
                {
                    output.Errors = childErrors;
                }
                if (childAnnotations != null)
                {
                    output.Annotations = childAnnotations;
                }
            }

            // Ensure Error is always set when Valid is false
            if (!IsValid)
            {
                output.Error = Error ?? GetSummaryError(output.Errors?.Count ?? 0);
            }

            if (Annotations != null && Annotations.Count > 0)
            {
                output.Annotation = Annotations;
            }

            return output;
        }

        private static string GetSummaryError(int errorCount)
        {
            if (errorCount == 0)
            {
                return "Validation failed";
            }
            if (errorCount == 1)
            {
                return "Validation failed with 1 error";
            }
            return $"Validation failed with {errorCount.ToString(System.Globalization.CultureInfo.InvariantCulture)} errors";
        }
    }
}
