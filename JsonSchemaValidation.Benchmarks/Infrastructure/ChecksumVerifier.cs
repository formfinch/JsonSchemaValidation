// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace FormFinch.JsonSchemaValidation.Benchmarks.Infrastructure;

/// <summary>
/// Verifies SHA-256 checksums of embedded benchmark data files to ensure integrity.
/// </summary>
public static class ChecksumVerifier
{
    /// <summary>
    /// Verifies all embedded resources against their expected checksums.
    /// Returns a list of verification failures.
    /// </summary>
    public static IReadOnlyList<string> VerifyAll()
    {
        var failures = new List<string>();
        var checksums = BenchmarkData.GetChecksums();
        var resources = BenchmarkData.GetAllResourceNames();

        foreach (var resource in resources)
        {
            // Skip the checksums file itself
            if (resource == "checksums.json")
            {
                continue;
            }

            if (!checksums.TryGetValue(resource, out var expectedChecksum))
            {
                failures.Add($"No checksum found for: {resource}");
                continue;
            }

            var content = BenchmarkData.GetResource(resource);
            var actualChecksum = ComputeSha256(content);

            if (!string.Equals(expectedChecksum, actualChecksum, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"Checksum mismatch for {resource}: expected {expectedChecksum}, got {actualChecksum}");
            }
        }

        return failures;
    }

    /// <summary>
    /// Verifies all embedded resources and throws if any verification fails.
    /// </summary>
    public static void VerifyAllOrThrow()
    {
        var failures = VerifyAll();
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"Benchmark data integrity check failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }
    }

    /// <summary>
    /// Computes SHA-256 hash of the given content.
    /// </summary>
    public static string ComputeSha256(string content)
    {
        // Normalize line endings to LF for consistent cross-platform checksums
        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Generates checksums for all embedded resources.
    /// Useful for updating checksums.json when data files change.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GenerateChecksums()
    {
        var checksums = new Dictionary<string, string>(StringComparer.Ordinal);
        var resources = BenchmarkData.GetAllResourceNames();

        foreach (var resource in resources)
        {
            // Skip the checksums file itself
            if (resource == "checksums.json")
            {
                continue;
            }

            var content = BenchmarkData.GetResource(resource);
            checksums[resource] = ComputeSha256(content);
        }

        return checksums;
    }
}
