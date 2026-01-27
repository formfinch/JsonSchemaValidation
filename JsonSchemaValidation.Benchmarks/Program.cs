// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;
using FormFinch.JsonSchemaValidation.Benchmarks.Infrastructure;

namespace FormFinch.JsonSchemaValidation.Benchmarks;

/// <summary>
/// Entry point for BenchmarkDotNet benchmarks.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        // Verify benchmark data integrity before running any benchmarks
        ChecksumVerifier.VerifyAllOrThrow();

        // Run benchmarks using BenchmarkSwitcher to allow filtering
        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args);
    }
}
