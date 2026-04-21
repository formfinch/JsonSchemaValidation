// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Keywords;

/// <summary>
/// Shared numeric parsing helpers for JS keyword emitters.
/// Centralises range/integer checks so every bounded-length keyword handles
/// out-of-range schema values the same way.
/// </summary>
internal static class JsSchemaNumeric
{
    /// <summary>
    /// Parses a JSON number as a non-negative integer count suitable for a
    /// length/bound keyword (minLength, maxItems, minContains, etc.). Returns
    /// false when the value isn't a number, is negative, is fractional, is
    /// non-finite, or exceeds long.MaxValue — an explicit range check before
    /// the double→long cast, since unchecked conversion outside range is
    /// unspecified in C# and can emit bogus constraints into the JS output.
    /// </summary>
    public static bool TryGetNonNegativeIntegerValue(JsonElement element, out long value)
    {
        value = 0;
        if (element.ValueKind != JsonValueKind.Number) return false;
        if (!element.TryGetDouble(out var d)) return false;
        if (!double.IsFinite(d)) return false;
        if (d < 0) return false;
        if (Math.Abs(d - Math.Floor(d)) > double.Epsilon) return false;
        if (d > long.MaxValue) return false;
        value = (long)d;
        return true;
    }
}
