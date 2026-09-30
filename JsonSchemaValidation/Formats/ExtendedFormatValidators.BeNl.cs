// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Formats;

internal static partial class ExtendedFormatValidators
{
    // RDW sidecodes 1-14 as letter (L) / digit (D) patterns.
    private static readonly HashSet<string> NlPlateSidecodes = new(StringComparer.Ordinal)
    {
        "LLDDDD", "DDDDLL", "DDLLDD", "LLDDLL", "LLLLDD", "DDLLLL", "DDLLLD",
        "DLLLDD", "LLDDDL", "LDDDLL", "LLLDDL", "LDDLLL", "DLLDDD", "DDDLLD",
    };

    /// <summary>Belgian postcode: 4 digits, the first not 0.</summary>
    /// <param name="value">The string to check. No separators.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Belgian postcode.</returns>
    public static bool IsValidBePostcode(string value) => IsFourDigitPostcode(value);

    /// <summary>Dutch licence plate (kenteken): 6 letters and digits in one of the RDW sidecodes 1–14.</summary>
    /// <param name="value">The string to check. Any case; hyphens and spaces allowed.</param>
    /// <returns><see langword="true"/> if the value is a structurally valid Dutch licence plate.</returns>
    public static bool IsValidNlPlate(string value)
    {
        return TryCompact(value, SpacesAndDashes, allowLetters: true, out var plate)
            && plate.Length == 6
            && NlPlateSidecodes.Contains(Shape(plate));
    }
}
