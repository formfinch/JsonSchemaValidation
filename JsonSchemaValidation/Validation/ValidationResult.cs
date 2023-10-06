using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Validation
{
    public class ValidationResult
    {
        public static readonly ValidationResult Ok = new() { IsValid = true };

        public bool IsValid { get; private set; } = true;
        public List<string> Errors { get; } = new List<string>();

        // Initialize a ValidationResult with a single error
        public ValidationResult(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        public ValidationResult() { }

        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        public void Merge(ValidationResult other)
        {
            if (other == Ok) return;

            IsValid &= other.IsValid;
            if (!other.IsValid)
            {
                Errors.AddRange(other.Errors);
            }
        }
    }
}
