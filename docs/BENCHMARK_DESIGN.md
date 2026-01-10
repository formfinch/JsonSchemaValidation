# JSON Schema Validation Benchmark Design

**Status:** Design phase - not yet implemented
**Created:** 2026-01-03

---

## Objective

Compare the performance of this solution against other JSON Schema validators to understand:
1. Where we stand relative to established libraries
2. Performance bottlenecks to optimize
3. Trade-offs between features and speed

---

## Comparison Targets

### .NET Libraries (BenchmarkDotNet)

| Library | Package | Why Include |
|---------|---------|-------------|
| **This solution** | `JsonSchemaValidation` | Baseline |
| **JsonSchema.Net** | `JsonSchema.Net` | Direct competitor, System.Text.Json based, feature-rich |
| **Lateapexearlyspeed.JsonSchema** | `LateApexEarlySpeed.Json.Schema` | Claims best .NET performance, System.Text.Json based |

Optional additions:
- **NJsonSchema** (`NJsonSchema`) - Popular but uses Newtonsoft.Json, different foundation
- **Json.NET Schema** (`Newtonsoft.Json.Schema`) - Commercial license required

### JavaScript (Node.js)

| Library | Package | Why Include |
|---------|---------|-------------|
| **Ajv** | `ajv` | Industry gold standard, compiles schemas to code |

---

## Benchmark Scenarios

### 1. Schema Loading (Cold Path)
Measure time to parse and prepare a schema for validation.

```
Scenario: Load schema from JSON string, prepare for validation
Metrics: Time, Memory allocation
```

### 2. First Validation (Cold Path)
Measure time for first validation including any lazy initialization.

```
Scenario: Load schema + validate one instance
Metrics: Time, Memory allocation
```

### 3. Repeated Validation (Hot Path)
Measure throughput when schema is cached/reused.

```
Scenario: Validate N instances against pre-loaded schema
Metrics: Operations/second, Memory allocation per validation
```

### 4. Large Document Validation
Measure performance with large JSON documents.

```
Scenario: Validate documents with 1K, 10K, 100K properties/items
Metrics: Time, Memory, Scaling factor
```

### 5. Complex Schema Validation
Measure performance with deeply nested schemas using advanced keywords.

```
Scenario: Schema with allOf/anyOf/oneOf, $ref chains, unevaluatedProperties
Metrics: Time, Memory
```

### 6. Format Validation
Measure format validator overhead when enabled.

```
Scenario: Validate strings against various formats (uri, email, date-time, etc.)
Metrics: Time per format
```

---

## Test Data

### Schemas

Located in `benchmarks/schemas/`:

```
simple-object.json       # Basic object with required properties
nested-object.json       # 5 levels of nesting
array-items.json         # Array with item validation
refs-chain.json          # Multiple $ref resolutions
allof-complex.json       # Complex allOf/anyOf/oneOf combinations
formats.json             # All format validators
draft2020-full.json      # Uses most 2020-12 features
```

### Instances

Located in `benchmarks/instances/`:

```
valid/
  small.json             # ~10 properties
  medium.json            # ~100 properties
  large.json             # ~10,000 properties
  array-small.json       # 100 items
  array-large.json       # 10,000 items

invalid/
  type-mismatch.json     # Fails early (type)
  nested-error.json      # Fails deep in structure
  format-error.json      # Fails on format
```

---

## Project Structure

```
JsonSchemaValidation.Benchmarks/
├── JsonSchemaValidation.Benchmarks.csproj
├── Program.cs
├── Benchmarks/
│   ├── SchemaLoadingBenchmarks.cs
│   ├── ValidationBenchmarks.cs
│   ├── LargeDocumentBenchmarks.cs
│   ├── ComplexSchemaBenchmarks.cs
│   └── FormatBenchmarks.cs
├── Comparisons/
│   ├── JsonSchemaNetAdapter.cs
│   └── LateapexAdapter.cs
└── Data/
    ├── schemas/
    └── instances/

benchmarks-js/
├── package.json
├── ajv-benchmark.js
└── data/               # Symlink or copy of shared test data
    ├── schemas/
    └── instances/
```

---

## .NET Benchmark Project

### Dependencies

```xml
<PackageReference Include="BenchmarkDotNet" Version="0.14.*" />
<PackageReference Include="JsonSchema.Net" Version="7.*" />
<PackageReference Include="LateApexEarlySpeed.Json.Schema" Version="*" />
<ProjectReference Include="..\JsonSchemaValidation\JsonSchemaValidation.csproj" />
```

### Example Benchmark Class

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ValidationBenchmarks
{
    private JsonElement _instance;
    private JsonElement _schema;

    // This solution
    private ISchemaValidator _thisValidator;

    // JsonSchema.Net
    private JsonSchema _jsonSchemaNet;

    [GlobalSetup]
    public void Setup()
    {
        var schemaJson = File.ReadAllText("Data/schemas/simple-object.json");
        var instanceJson = File.ReadAllText("Data/instances/valid/medium.json");

        _schema = JsonDocument.Parse(schemaJson).RootElement;
        _instance = JsonDocument.Parse(instanceJson).RootElement;

        // Setup this solution
        var options = new SchemaValidationOptions();
        var serviceProvider = SchemaDraft202012Setup.ConfigureServices(options);
        var repository = serviceProvider.GetRequiredService<ISchemaRepository>();
        repository.TryRegisterSchema(_schema, out var metadata);
        var factory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        _thisValidator = factory.GetValidator(metadata.SchemaUri);

        // Setup JsonSchema.Net
        _jsonSchemaNet = JsonSchema.FromText(schemaJson);
    }

    [Benchmark(Baseline = true)]
    public bool ThisSolution()
    {
        var context = new JsonValidationContext(_instance);
        return _thisValidator.Validate(context).IsValid;
    }

    [Benchmark]
    public bool JsonSchemaNet()
    {
        var result = _jsonSchemaNet.Evaluate(_instance);
        return result.IsValid;
    }
}
```

---

## Node.js Ajv Benchmark

### package.json

```json
{
  "name": "json-schema-benchmarks",
  "version": "1.0.0",
  "scripts": {
    "benchmark": "node ajv-benchmark.js"
  },
  "dependencies": {
    "ajv": "^8.12.0",
    "ajv-formats": "^2.1.1"
  }
}
```

### ajv-benchmark.js

```javascript
const Ajv = require('ajv');
const addFormats = require('ajv-formats');
const fs = require('fs');

const ajv = new Ajv({ allErrors: true });
addFormats(ajv);

const schema = JSON.parse(fs.readFileSync('data/schemas/simple-object.json'));
const instance = JSON.parse(fs.readFileSync('data/instances/valid/medium.json'));

// Compile schema (one-time cost)
const validate = ajv.compile(schema);

// Warmup
for (let i = 0; i < 1000; i++) {
    validate(instance);
}

// Benchmark
const iterations = 100000;
const start = process.hrtime.bigint();

for (let i = 0; i < iterations; i++) {
    validate(instance);
}

const end = process.hrtime.bigint();
const totalMs = Number(end - start) / 1_000_000;
const opsPerSecond = (iterations / totalMs) * 1000;

console.log(`Ajv Results:`);
console.log(`  Total time: ${totalMs.toFixed(2)}ms`);
console.log(`  Iterations: ${iterations}`);
console.log(`  Ops/second: ${opsPerSecond.toFixed(0)}`);
console.log(`  Mean: ${(totalMs / iterations * 1000).toFixed(3)}μs`);
```

---

## Running Benchmarks

### .NET

```bash
cd JsonSchemaValidation.Benchmarks
dotnet run -c Release -- --filter "*"
```

### Node.js (Ajv)

```bash
cd benchmarks-js
npm install
npm run benchmark
```

---

## Expected Output Format

### BenchmarkDotNet (example)

```
|        Method |       Mean |   Allocated |
|-------------- |-----------:|------------:|
| ThisSolution  |   2.345 μs |       512 B |
| JsonSchemaNet |   3.456 μs |       768 B |
| Lateapex      |   1.234 μs |       384 B |
```

### Ajv (example)

```
Ajv Results:
  Total time: 1234.56ms
  Iterations: 100000
  Ops/second: 81000
  Mean: 12.345μs
```

---

## Key Metrics to Compare

1. **Throughput** (ops/second) - Higher is better
2. **Latency** (μs/operation) - Lower is better
3. **Memory** (bytes/operation) - Lower is better
4. **Cold start** (first validation time) - Important for serverless
5. **Scaling** (how performance degrades with size)

---

## Known Performance Considerations

### This Solution - Potential Bottlenecks

1. **No validator caching** - `GetValidator()` creates new instances each call
2. **Interpreted validation** - No schema compilation like Ajv
3. **Factory pattern overhead** - 49 keyword factory classes
4. **Regex compilation** - Format validators compile regex on each instantiation

### Optimization Opportunities

1. Cache compiled validators per schema URI
2. Pre-compile regex patterns as static readonly
3. Object pooling for validation contexts
4. Consider source generators for hot paths

---

## Next Steps

1. [ ] Create the benchmark project structure
2. [ ] Implement benchmark classes
3. [ ] Create shared test data
4. [ ] Run initial benchmarks
5. [ ] Identify optimization opportunities
6. [ ] Implement optimizations
7. [ ] Re-run benchmarks to measure improvement
