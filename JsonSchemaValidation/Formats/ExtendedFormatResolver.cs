// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using FormFinch.JsonSchemaValidation.Abstractions.Keywords;

namespace FormFinch.JsonSchemaValidation.Formats
{
    /// <summary>
    /// Shared lookup used by every draft's format factory once format assertion is active.
    /// </summary>
    internal static class ExtendedFormatResolver
    {
        private static readonly Dictionary<string, IKeywordValidator> Validators = CreateValidators();

        public static bool TryGetValidator(SchemaValidationOptions options, string format, [NotNullWhen(true)] out IKeywordValidator? validator)
        {
            validator = null;
            return options.EnableExtendedFormats && Validators.TryGetValue(format, out validator);
        }

        private static Dictionary<string, IKeywordValidator> CreateValidators()
        {
            var validators = new Dictionary<string, IKeywordValidator>(StringComparer.Ordinal);
            foreach (var entry in ExtendedFormatValidators.Catalog)
            {
                validators[entry.Key] = new ExtendedFormatKeywordValidator(entry.Key, entry.Value);
            }
            return validators;
        }
    }
}
