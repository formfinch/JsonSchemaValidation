# Performance Plan: Closing the Gap with LateApex

## Current State

| Library | Median | Throughput | Memory |
|---------|--------|------------|--------|
| LateApex | 2.0 µs | 506K/s | 2,228 KB |
| JsonSchemaValidation | 4.7 µs | 214K/s | 8,079 KB |

**Gap:** 2.4x slower, 3.6x more memory

## Target

- Median time: < 2.0 µs
- Throughput: > 500K/s
- Memory: < 3,000 KB

---

## Analysis: Where Time is Spent

Based on the IsValid() fast path implementation, remaining allocations and overhead:

### 1. Context Object Allocation (High Impact)
Every property/array item traversal creates a `FastValidationContext`:
```csharp
var prpContext = _contextFactory.CreateContextForPropertyFast(context, value);
// Creates: new FastValidationContext(value, context.Scope)
```
For an object with 10 properties validated by 3 nested schemas = 30+ allocations.

### 2. Virtual Method Dispatch (Medium Impact)
Each keyword validator call goes through interface dispatch:
```csharp
foreach (var validator in _keywordValidators)
{
    if (!validator.IsValid(context))  // Virtual call
```

### 3. Dictionary Lookups (Medium Impact)
PropertiesValidator looks up validators by property name:
```csharp
foreach (string propertyName in _propertySchemaValidators.Keys)
{
    var validator = _propertySchemaValidators[propertyName];
```

### 4. Scope Stack Operations (Low-Medium Impact)
$ref resolution pushes/pops schema resources:
```csharp
context.Scope.PushSchemaResource(resolvedSchema);
// ... validate ...
context.Scope.PopSchemaResource();
```

### 5. Memory Overhead (High Impact)
3.6x more memory suggests excessive object creation or retention.

---

## Optimization Plan

### Phase 1: Eliminate Context Allocations (Target: 1.5x improvement)

#### 1.1 Use Struct Context with Passing Pattern
Replace class-based context with a `ref struct` that's stack-allocated:

```csharp
ref struct FastContext
{
    public JsonElement Data;
    public IValidationScope Scope;
}

// Usage - no allocation
FastContext ctx = new() { Data = value, Scope = context.Scope };
validator.IsValid(ref ctx);
```

**Challenge:** `ref struct` can't be stored in fields or used with interfaces.

**Solution:** Add `IsValid(JsonElement data, IValidationScope scope)` overload to validators.

#### 1.2 Pass JsonElement Directly for Simple Validators
Many validators only need the JsonElement, not the full context:
```csharp
// Current
bool IsValid(IJsonValidationContext context)

// Optimized for simple validators (type, const, enum, etc.)
bool IsValid(JsonElement data)
```

Type, Const, Enum, Pattern, Format validators can use this simpler signature.

### Phase 2: Reduce Virtual Dispatch (Target: 1.2x improvement)

#### 2.1 Specialized Schema Validators
Create specialized validators for common schema patterns:

```csharp
// Instead of generic SchemaValidator with list of keyword validators:
class TypeOnlyValidator : ISchemaValidator
{
    private readonly JsonValueKind _expectedKind;

    public bool IsValid(IJsonValidationContext context)
    {
        return context.Data.ValueKind == _expectedKind;
    }
}

class TypeAndPropertiesValidator : ISchemaValidator
{
    // Inline type check + properties validation
}
```

Common patterns to specialize:
- Type-only schemas: `{"type": "string"}`
- Type + properties: `{"type": "object", "properties": {...}}`
- Type + items: `{"type": "array", "items": {...}}`
- Enum schemas: `{"enum": [...]}`
- Const schemas: `{"const": ...}`

#### 2.2 Devirtualization via Sealed Classes
Ensure all validator classes are `sealed` and `internal` to help JIT devirtualize.

### Phase 3: Optimize Property Lookups (Target: 1.1x improvement)

#### 3.1 Use FrozenDictionary for Property Validators
.NET 8+ `FrozenDictionary` is optimized for read-heavy workloads:
```csharp
private readonly FrozenDictionary<string, ISchemaValidator> _propertyValidators;
```

#### 3.2 Property Name Interning
Intern property names to enable reference equality:
```csharp
// During schema compilation
propertyName = string.Intern(propertyName);

// During validation - faster lookup
if (ReferenceEquals(prp.Name, expectedName)) { ... }
```

### Phase 4: Reduce Scope Overhead (Target: 1.1x improvement)

#### 4.1 Scope-Free Validation Path
Most schemas don't use $dynamicRef. Add a flag to skip scope tracking:
```csharp
if (_requiresScopeTracking)
{
    context.Scope.PushSchemaResource(schema);
    try { return InnerValidate(context); }
    finally { context.Scope.PopSchemaResource(); }
}
else
{
    return InnerValidate(context);  // No scope overhead
}
```

#### 4.2 Inline Scope for Simple Refs
For simple $ref (non-dynamic), resolve at compile time and inline.

### Phase 5: Memory Optimization (Target: 3x reduction)

#### 5.1 Object Pooling for Contexts
If struct context isn't feasible, pool context objects:
```csharp
private static readonly ObjectPool<FastValidationContext> _contextPool =
    new DefaultObjectPool<FastValidationContext>(new ContextPolicy());

var ctx = _contextPool.Get();
try { return validator.IsValid(ctx); }
finally { _contextPool.Return(ctx); }
```

#### 5.2 Reduce Validator Object Graph
Analyze validator retention - ensure schemas aren't holding unnecessary references.

#### 5.3 Lazy Validator Creation
Don't create validators for keywords that won't be hit:
```csharp
// Instead of creating all validators upfront
// Create on first use for rarely-hit keywords
```

---

## Implementation Priority

| Phase | Optimization | Expected Impact | Effort | Priority |
|-------|--------------|-----------------|--------|----------|
| 1.2 | Pass JsonElement directly | 1.3x | Low | **1** |
| 2.2 | Sealed classes | 1.05x | Low | **2** |
| 3.1 | FrozenDictionary | 1.1x | Low | **3** |
| 1.1 | Struct context | 1.5x | Medium | **4** |
| 2.1 | Specialized validators | 1.2x | Medium | **5** |
| 4.1 | Scope-free path | 1.1x | Medium | **6** |
| 5.1 | Object pooling | 1.2x | Medium | **7** |

**Combined theoretical improvement: 2.4x - 3x**

---

## Quick Wins (Implement First)

### 1. Add `IsValid(JsonElement data)` to Simple Validators
Validators that only check the data value:
- TypeValidator
- ConstValidator
- EnumValidator
- MinLengthValidator / MaxLengthValidator
- MinimumValidator / MaximumValidator
- PatternValidator
- FormatValidators (all 19)

### 2. Seal All Validator Classes
Add `sealed` modifier to all internal validator classes.

### 3. Use FrozenDictionary
Replace `Dictionary<string, ISchemaValidator>` with `FrozenDictionary`.

---

## Measurement Plan

After each optimization:
1. Run full benchmark: `dotnet run -c Release -- -l jsonschemavalidation -l lateapex -i 1000`
2. Compare median time, throughput, memory
3. Track wins against LateApex specifically
4. Document results in this file

---

## Success Criteria

- [ ] Median time < 2.0 µs
- [ ] Throughput > 500K/s
- [ ] Memory < 3,000 KB
- [ ] Win rate vs LateApex > 50%

---

*Plan created: 2025-01-10*
