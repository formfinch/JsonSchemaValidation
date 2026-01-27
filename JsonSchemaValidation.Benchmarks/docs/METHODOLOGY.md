# Benchmark Methodology

This document describes the methodology used for benchmarking JSON Schema validators.

## Benchmarking Framework

All benchmarks use [BenchmarkDotNet](https://benchmarkdotnet.org/), the standard .NET benchmarking library that provides:

- Statistical rigor with confidence intervals
- Warmup and iteration management
- GC and memory allocation tracking
- Consistent execution environment

## Benchmark Categories

### 1. Parsing Benchmarks

Measure the time to parse/compile a JSON schema.

- **Cold Parsing**: First-run cost including all initialization
- **Warm Parsing**: Steady-state parsing with pre-warmed infrastructure

### 2. Validation Benchmarks

Measure the time to validate a JSON instance against a prepared schema.

- **Simple Validation**: Minimal schema (type only)
- **Complex Validation**: Logic combinators (allOf, anyOf, oneOf) with $ref
- **Large Document Validation**: Production-grade schemas (GitHub Workflow)

### 3. Throughput Benchmarks

Measure sustained validations per second using batch operations.

### 4. Cross-Draft Benchmarks

Compare performance across JSON Schema drafts (Draft 4, 6, 7, 2019-09, 2020-12) using a cross-compatible schema.

### 5. Competitor Benchmarks

Compare FormFinch against other .NET JSON Schema validators:
- JsonSchema.Net
- LateApexEarlySpeed.Json.Schema

## Test Data

All benchmark data is embedded in the assembly and verified via SHA-256 checksums at startup. This ensures:

- Reproducible results across runs
- No filesystem I/O during benchmarks
- Tamper detection

### Schema Complexity Levels

| Level | Description | Keywords |
|-------|-------------|----------|
| Simple | Single type constraint | `type`, `minLength` |
| Medium | Object with properties | `properties`, `required`, `pattern` |
| Complex | Nested with combinators | `allOf`, `anyOf`, `oneOf`, `$ref` |
| Production | Real-world schema | GitHub Workflow schema |

## Configuration

### Default Configuration

- Warmup: 3 iterations
- Measurement: 15 iterations
- Memory diagnostics enabled
- Output: Markdown and JSON

### Cold Run Configuration

- Strategy: ColdStart
- Invocation count: 1
- Unroll factor: 1
- No warmup

### Throughput Configuration

- Warmup: 5 iterations
- Measurement: 20 iterations
- Operations per invoke: 1000

## Running Benchmarks

```bash
# Run all benchmarks
dotnet run -c Release --project JsonSchemaValidation.Benchmarks

# Run specific benchmark class
dotnet run -c Release --project JsonSchemaValidation.Benchmarks -- --filter *Simple*

# Dry run (verification only)
dotnet run -c Release --project JsonSchemaValidation.Benchmarks -- --filter *Simple* --job dry
```

## Interpreting Results

BenchmarkDotNet provides several statistics:

- **Mean**: Average execution time
- **Median**: Middle value (more robust to outliers)
- **StdDev**: Standard deviation
- **P95**: 95th percentile
- **Allocated**: Memory allocations per operation

### Comparison Notes

1. **FormFinch Dynamic vs Compiled**: Dynamic uses the interpreter; Compiled generates native code at runtime
2. **Memory**: Lower allocations typically indicate better performance for high-throughput scenarios
3. **.NET Only**: Benchmarks compare .NET JSON Schema validators only
