// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Common
{
    /// <summary>
    /// Lightweight validation context for the IsValid() fast path.
    /// Skips instance location tracking and unevaluated properties initialization.
    /// </summary>
    internal sealed class FastValidationContext : IJsonValidationContext
    {
        private static readonly JsonPointer NoLocation = JsonPointer.Empty;

        private readonly JsonElement _data;
        private readonly IValidationScope _scope;

        public FastValidationContext(JsonElement data, IValidationScope scope)
        {
            _data = data;
            _scope = scope;
        }

        public JsonElement Data => _data;

        public IValidationScope Scope => _scope;

        // Return empty pointer - not tracked in fast path
        public JsonPointer InstanceLocation => NoLocation;
    }
}
