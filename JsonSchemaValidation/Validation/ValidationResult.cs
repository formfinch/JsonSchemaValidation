namespace JsonSchemaValidation.Validation
{
    public class ValidationResult
    {
        public static readonly ValidationResult Ok = new() { IsValid = true };

        public bool IsValid { get; private set; } = true;
        public List<string> Errors { get; } = new List<string>();
        public Dictionary<string, object?> Annotations { get; } = new Dictionary<string, object?>();

        // Initialize a ValidationResult with a single error
        public ValidationResult(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        public ValidationResult() { }

        public void AddError(string error)
        {
            if (this == ValidationResult.Ok)
            {
                throw new InvalidOperationException("Not allowed to change ValidationResult.Ok");
            }

            IsValid = false;
            Errors.Add(error);
        }

        public void SetAnnotation(string keyword, object? value)
        {
            if (this == ValidationResult.Ok)
            {
                throw new InvalidOperationException("Not allowed to change ValidationResult.Ok");
            }

            Annotations[keyword] = value;
        }

        public void Merge(ValidationResult other)
        {
            if (this == ValidationResult.Ok)
            {
                throw new InvalidOperationException("Not allowed to change ValidationResult.Ok");
            }

            if (other == Ok) return;

            IsValid &= other.IsValid;
            if (!other.IsValid)
            {
                Errors.AddRange(other.Errors);
            }

            foreach (var annotation in other.Annotations)
            {
                Annotations[annotation.Key] = annotation.Value;
            }
        }
    }
}
