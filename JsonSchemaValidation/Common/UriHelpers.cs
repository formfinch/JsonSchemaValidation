// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Common;

/// <summary>
/// URI helpers for JSON Schema processing.
/// </summary>
internal static class UriHelpers
{
    /// <summary>
    /// Tries to create an absolute URI, rejecting file:// URIs that result from
    /// Linux treating /-prefixed paths as absolute file paths.
    /// JSON Schema URIs are always http/https/urn — never file.
    /// </summary>
    public static bool TryCreateAbsoluteSchemaUri(string uriString, out Uri result)
    {
        if (Uri.TryCreate(uriString, UriKind.Absolute, out result!))
        {
            if (string.Equals(result.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                result = null!;
                return false;
            }

            return true;
        }

        result = null!;
        return false;
    }
}
