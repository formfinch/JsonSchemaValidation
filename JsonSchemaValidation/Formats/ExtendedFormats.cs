// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidation.Formats;

/// <summary>
/// Entry point for compiled validators generated with extended formats enabled
/// (<c>jsv-codegen generate --extended-formats</c>). Generated code lives in the caller's
/// assembly and reaches the extended format catalog through this method.
/// </summary>
public static class ExtendedFormats
{
    /// <summary>Checks a JSON value against an extended catalog format.</summary>
    /// <param name="format">The catalog format name, e.g. <c>"nl-phone"</c>.</param>
    /// <param name="data">The JSON value to check.</param>
    /// <returns>
    /// <see langword="true"/> if the value is acceptable for the format or is not a string;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The format is not in this version's catalog, which means the validator was generated
    /// with a newer catalog than the library it runs against.
    /// </exception>
    public static bool IsValid(string format, JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.String)
            return true;
        var value = data.GetString();
        return value is null || ExtendedFormatValidators.IsValid(format, value);
    }
}
