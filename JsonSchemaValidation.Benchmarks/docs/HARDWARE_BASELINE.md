# Hardware Baseline

This document records the hardware and software baseline for benchmark results.

## Automatic Detection

BenchmarkDotNet automatically detects and reports:

- Processor model and frequency
- .NET runtime version
- OS version and platform
- JIT compiler version
- GC configuration

This information appears in the benchmark output header.

## Baseline Environment

Results in this repository were collected on:

| Component | Specification |
|-----------|---------------|
| **Processor** | [To be filled by benchmark run] |
| **Memory** | [To be filled by benchmark run] |
| **OS** | [To be filled by benchmark run] |
| **.NET SDK** | [To be filled by benchmark run] |
| **Runtime** | [To be filled by benchmark run] |

## Reproducing Results

For accurate comparison with published results:

1. Run benchmarks in **Release** configuration
2. Close other applications
3. Disable power saving modes
4. Run multiple times to verify stability
5. Use consistent hardware

## Environment Variables

The following environment variables affect benchmark results:

```bash
# Disable tiered compilation for more stable results
DOTNET_TieredCompilation=0

# Disable ready-to-run to measure JIT overhead
DOTNET_ReadyToRun=0
```

## Benchmark Hygiene

- All benchmarks run with `Release` configuration
- JIT warmup is performed before measurement
- Results are collected over multiple iterations
- Statistical analysis removes outliers
- Memory allocations are tracked per operation
