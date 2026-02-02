// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Polyfills;

/// <summary>
/// Provides helper methods for JsonElement operations with multi-target compatibility.
/// </summary>
internal static class JsonElementHelper
{
    /// <summary>
    /// Compares two JSON elements for deep equality.
    /// On .NET 9+, delegates to the native JsonElement.DeepEquals method.
    /// On earlier versions, uses a custom implementation.
    /// </summary>
#if NET9_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DeepEquals(JsonElement left, JsonElement right)
        => JsonElement.DeepEquals(left, right);
#else
    public static bool DeepEquals(JsonElement left, JsonElement right)
        => DeepEqualsCompat(left, right);
#endif

    /// <summary>
    /// Custom deep equality comparison for JsonElement.
    /// Always available for testing regardless of runtime version.
    /// </summary>
    internal static bool DeepEqualsCompat(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        return left.ValueKind switch
        {
            JsonValueKind.Null => true,
            JsonValueKind.True => true,
            JsonValueKind.False => true,
            JsonValueKind.String => string.Equals(left.GetString(), right.GetString(), StringComparison.Ordinal),
            JsonValueKind.Number => CompareNumbers(left, right),
            JsonValueKind.Array => CompareArrays(left, right),
            JsonValueKind.Object => CompareObjects(left, right),
            _ => false
        };
    }

    private static bool CompareNumbers(JsonElement left, JsonElement right)
    {
        // Per JSON specification, numbers are compared by their mathematical value.
        // 0, 0.0, and 0.00 are all equal, as are 1e2 and 100.
        // First try raw text comparison (fast path for identical representations).
        var leftRaw = left.GetRawText();
        var rightRaw = right.GetRawText();

        if (string.Equals(leftRaw, rightRaw, StringComparison.Ordinal))
        {
            return true;
        }

        // Try decimal comparison for numeric equality
        if (left.TryGetDecimal(out var leftDecimal) && right.TryGetDecimal(out var rightDecimal))
        {
            return leftDecimal == rightDecimal;
        }

        // Fallback to double comparison for very large numbers
        if (left.TryGetDouble(out var leftDouble) && right.TryGetDouble(out var rightDouble))
        {
            // Handle infinity and NaN
            if (double.IsNaN(leftDouble) && double.IsNaN(rightDouble))
            {
                return true;
            }

            return leftDouble.Equals(rightDouble);
        }

        return false;
    }

    private static bool CompareArrays(JsonElement left, JsonElement right)
    {
        var leftLength = left.GetArrayLength();
        var rightLength = right.GetArrayLength();

        if (leftLength != rightLength)
        {
            return false;
        }

        using var leftEnumerator = left.EnumerateArray();
        using var rightEnumerator = right.EnumerateArray();

        while (leftEnumerator.MoveNext() && rightEnumerator.MoveNext())
        {
            if (!DeepEqualsCompat(leftEnumerator.Current, rightEnumerator.Current))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CompareObjects(JsonElement left, JsonElement right)
    {
        // Count properties
        var leftCount = 0;
        foreach (var _ in left.EnumerateObject())
        {
            leftCount++;
        }

        var rightCount = 0;
        foreach (var _ in right.EnumerateObject())
        {
            rightCount++;
        }

        if (leftCount != rightCount)
        {
            return false;
        }

        // Compare each property from left in right
        foreach (var leftProperty in left.EnumerateObject())
        {
            if (!right.TryGetProperty(leftProperty.Name, out var rightValue))
            {
                return false;
            }

            if (!DeepEqualsCompat(leftProperty.Value, rightValue))
            {
                return false;
            }
        }

        return true;
    }
}
